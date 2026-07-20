using System.Text.Json;
using System.Text.RegularExpressions;

namespace Rafiq.Application.VoiceAgent.Agent;

public static class AgentResponseParser
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AgentOutput Parse(string rawResponse)
    {
        var json = ExtractJson(rawResponse);
        if (json is null)
            return new FinalAnswer(rawResponse.Trim(), null);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("action", out var actionProp))
                return new FinalAnswer(rawResponse.Trim(), null);

            var action = actionProp.GetString() ?? string.Empty;

            // Some models output {"action":"<tool_name>","tool":"<tool_name>","parameters":{...}}
            // instead of the correct {"action":"tool_call","tool":"<tool_name>","parameters":{...}}.
            // Detect this: if action isn't one of the three known values but parameters exist,
            // treat it as a tool_call where the tool name is taken from "tool" or from "action".
            if (action is not ("tool_call" or "ask_user" or "final_answer"))
            {
                if (root.TryGetProperty("parameters", out _))
                {
                    var recoveredTool = root.TryGetProperty("tool", out var tProp)
                        ? tProp.GetString() ?? action
                        : action;
                    return ParseToolCallWithName(root, recoveredTool);
                }

                return new FinalAnswer(rawResponse.Trim(), null);
            }

            return action switch
            {
                "tool_call" => ParseToolCall(root),
                "ask_user" => ParseAskUser(root),
                "final_answer" => ParseFinalAnswer(root),
                _ => new FinalAnswer(rawResponse.Trim(), null)
            };
        }
        catch
        {
            return new FinalAnswer(rawResponse.Trim(), null);
        }
    }

    private static AgentOutput ParseToolCall(JsonElement root)
    {
        var toolName = root.TryGetProperty("tool", out var t) ? t.GetString() ?? string.Empty : string.Empty;
        return ParseToolCallWithName(root, toolName);
    }

    private static ToolCall ParseToolCallWithName(JsonElement root, string toolName)
    {
        var parameters = root.TryGetProperty("parameters", out var p)
            ? p.Clone()
            : JsonDocument.Parse("{}").RootElement;

        return new ToolCall(toolName, parameters);
    }

    private static AgentOutput ParseAskUser(JsonElement root)
    {
        var question = root.TryGetProperty("question", out var q) ? q.GetString() ?? string.Empty : string.Empty;
        return new AskUser(question);
    }

    private static AgentOutput ParseFinalAnswer(JsonElement root)
    {
        var response = root.TryGetProperty("response", out var r) ? r.GetString() ?? string.Empty : string.Empty;
        var navigateTo = root.TryGetProperty("navigate_to", out var n) ? n.GetString() : null;
        return new FinalAnswer(response, navigateTo);
    }

    // Extracts the first {...} block from raw text, handling markdown code fences
    private static string? ExtractJson(string text)
    {
        // Try to strip markdown ```json ... ``` fences
        var fenceMatch = Regex.Match(text, @"```(?:json)?\s*(\{[\s\S]*?\})\s*```");
        if (fenceMatch.Success)
            return fenceMatch.Groups[1].Value;

        // Find the first { and the matching }
        var start = text.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return text[start..(i + 1)];
            }
        }

        return null;
    }
}
