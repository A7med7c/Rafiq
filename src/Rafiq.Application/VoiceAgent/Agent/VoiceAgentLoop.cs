using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rafiq.Application.AI.Models;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.VoiceAgent.Prompts;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.VoiceAgent.Agent;

public sealed class VoiceAgentLoop(
    IAiChatService aiService,
    ToolRegistry toolRegistry,
    INotificationService notificationService,
    ILogger<VoiceAgentLoop> logger)
{
    private const int MaxIterations = 8;

    // camelCase so the AI sees the field names it's instructed to look for in the prompt
    // ("success", "message", "data", "entityType", "completenessGaps").
    // No WhenWritingNull — null values in tool Data payloads must reach the AI as explicit
    // nulls (e.g. "height": null) so it never invents a value. Outer ToolResult fields use
    // per-property [JsonIgnore] instead.
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    public Task<(AgentOutput Output, IReadOnlyList<(AiMessageRole Role, string Content)> NewMessages)>
        RunAsync(
            string userMessage,
            IEnumerable<(AiMessageRole Role, string Content)> existingHistory,
            AgentContext ctx,
            CancellationToken ct)
        => RunAsync(userMessage, existingHistory, ctx, systemPromptOverride: null, base64Image: null, imageFormat: null, ct);

    /// <summary>
    /// Full form. Chat callers pass a Chat-specific system prompt override and optionally
    /// an image; the image is attached only to the FIRST Bedrock iteration (subsequent
    /// tool-result iterations are pure text reasoning and don't need the image re-sent).
    /// </summary>
    public async Task<(AgentOutput Output, IReadOnlyList<(AiMessageRole Role, string Content)> NewMessages)>
        RunAsync(
            string userMessage,
            IEnumerable<(AiMessageRole Role, string Content)> existingHistory,
            AgentContext ctx,
            string? systemPromptOverride,
            string? base64Image,
            string? imageFormat,
            CancellationToken ct)
    {
        var tools = toolRegistry.GetAll();
        var systemPrompt = systemPromptOverride
            ?? VoiceAgentSystemPrompt.Build(tools, ctx);

        var history = existingHistory.ToList();
        history.Add((AiMessageRole.User, userMessage));

        var newMessages = new List<(AiMessageRole Role, string Content)>();

        for (var i = 0; i < MaxIterations; i++)
        {
            var aiMessages = history.Select(m => new AiChatMessageDto
            {
                Role = m.Role switch
                {
                    AiMessageRole.ToolCall   => AiMessageRole.Assistant,
                    AiMessageRole.ToolResult => AiMessageRole.User,
                    _                        => m.Role
                },
                Content = m.Content
            }).ToList();

            var lastMessage = aiMessages.Last();
            var previousMessages = aiMessages.Take(aiMessages.Count - 1).ToList();

            var request = new AiChatRequest
            {
                SystemPrompt = systemPrompt,
                PreviousMessages = previousMessages,
                CurrentUserMessage = lastMessage.Content,
                MaxOutputTokens = 1024,
                // Only attach image on the first iteration — subsequent iterations are
                // pure text reasoning over tool results and re-sending the image wastes tokens.
                Base64Image = i == 0 ? base64Image : null,
                ImageFormat = i == 0 ? imageFormat : null,
            };

            AiChatResponse aiResponse;
            try
            {
                aiResponse = await aiService.GenerateResponseAsync(request, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI service call failed on iteration {Iteration}", i + 1);
                return (new FinalAnswer("I'm having trouble connecting to the AI service. Please try again.", null), newMessages);
            }

            var rawContent = aiResponse.Content ?? string.Empty;
            logger.LogDebug("VoiceAgentLoop iteration {Iter}: raw={Raw}", i + 1, rawContent);

            var output = AgentResponseParser.Parse(rawContent);

            switch (output)
            {
                case FinalAnswer fa:
                {
                    // Prefer the extracted response; fall back to raw content so the
                    // conversation is always persisted even when the AI omits the JSON field.
                    var content = !string.IsNullOrWhiteSpace(fa.Response)
                        ? fa.Response
                        : !string.IsNullOrWhiteSpace(rawContent) ? rawContent.Trim() : "…";
                    newMessages.Add((AiMessageRole.Assistant, content));
                    return (new FinalAnswer(content, fa.NavigateTo), newMessages);
                }

                case AskUser au:
                {
                    var content = !string.IsNullOrWhiteSpace(au.Question)
                        ? au.Question
                        : !string.IsNullOrWhiteSpace(rawContent) ? rawContent.Trim() : "…";
                    newMessages.Add((AiMessageRole.Assistant, content));
                    return (new AskUser(content), newMessages);
                }

                case ToolCall tc:
                    newMessages.Add((AiMessageRole.ToolCall, rawContent));
                    history.Add((AiMessageRole.ToolCall, rawContent));

                    // Push a real-time "thinking" event so the UI can show a spinner.
                    // Notification failures must never interrupt the agent loop.
                    try
                    {
                        await notificationService.SendVoiceAgentThinkingAsync(
                            ctx.UserId.ToString(),
                            new() { ToolName = tc.ToolName },
                            ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "VoiceAgentThinking notification failed (non-fatal)");
                    }

                    // Family profile context switching: if the AI includes "targetProfileId"
                    // in any tool's parameters, route that call to the specified profile.
                    // This lets all existing tools (add_medication, book_appointment, etc.)
                    // operate on a family member's profile without any change to those tools.
                    var effectiveCtx = ctx;
                    if (tc.Parameters.TryGetProperty("targetProfileId", out var tpidProp)
                        && Guid.TryParse(tpidProp.GetString(), out var familyProfileId))
                    {
                        effectiveCtx = ctx with { ProfileId = familyProfileId };
                        logger.LogDebug("targetProfileId override → routing tool '{Tool}' to profile {ProfileId}",
                            tc.ToolName, familyProfileId);
                    }

                    string toolResultContent;
                    var tool = toolRegistry.Get(tc.ToolName);

                    if (tool is null)
                    {
                        logger.LogWarning("Tool '{ToolName}' not found in registry", tc.ToolName);
                        toolResultContent = JsonSerializer.Serialize(
                            new { success = false, message = $"Tool '{tc.ToolName}' is not available." });
                    }
                    else
                    {
                        try
                        {
                            // ToolRegistry.ExecuteAsync runs the tool AND the entity analyzer —
                            // gaps are attached to the result so the AI can reason about them.
                            var result = await toolRegistry.ExecuteAsync(
                                tool,
                                new ToolCallRequest(tc.ToolName, tc.Parameters),
                                effectiveCtx,
                                ct);

                            toolResultContent = JsonSerializer.Serialize(result, _jsonOpts);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Tool '{ToolName}' threw an exception", tc.ToolName);
                            toolResultContent = JsonSerializer.Serialize(
                                new { success = false, message = $"Tool execution failed: {ex.Message}" });
                        }
                    }

                    newMessages.Add((AiMessageRole.ToolResult, toolResultContent));
                    history.Add((AiMessageRole.ToolResult, toolResultContent));
                    break;
            }
        }

        logger.LogWarning("VoiceAgentLoop reached max iterations ({Max}) for session {SessionId}",
            MaxIterations, ctx.SessionId);
        return (new FinalAnswer("I wasn't able to complete that in time. Please try again.", null), newMessages);
    }
}
