using System.Diagnostics;
using Microsoft.Extensions.Options;
using Rafiq.Application.AI.Models;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;
using Rafiq.Infrastructure.Services.AiChat;

namespace Rafiq.Infrastructure.Services.Ai;

/// <summary>
/// Decorator around <see cref="AiChatService"/> that records a telemetry row
/// for every chat/voice AI call.
/// </summary>
public sealed class InstrumentedAiChatService(
    AiChatService           inner,
    IAiTelemetryContext     telemetryContext,
    IAiRequestLogWriter     logWriter,
    IOptions<AiChatSettings> chatSettings) : IAiChatService
{
    public async Task<AiChatResponse> GenerateResponseAsync(
        AiChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var feature = telemetryContext.Feature ?? AiFeature.Chat;
        var settings = chatSettings.Value;
        var modelName = string.IsNullOrWhiteSpace(request.Base64Image)
            ? settings.ModelId
            : settings.MultimodalModelId;

        var sw = Stopwatch.StartNew();

        try
        {
            var result = await inner.GenerateResponseAsync(request, cancellationToken);
            sw.Stop();

            await logWriter.WriteAsync(
                feature, modelName ?? "unknown", success: true, (int)sw.ElapsedMilliseconds,
                userId:         telemetryContext.UserId,
                conversationId: telemetryContext.ConversationId);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();

            await logWriter.WriteAsync(
                feature, modelName ?? "unknown", success: false, (int)sw.ElapsedMilliseconds,
                userId:         telemetryContext.UserId,
                errorType:      ex.GetType().Name,
                conversationId: telemetryContext.ConversationId);

            throw;
        }
    }
}
