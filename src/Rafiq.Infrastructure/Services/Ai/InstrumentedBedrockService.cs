using System.Diagnostics;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Enums;

namespace Rafiq.Infrastructure.Services.Ai;

/// <summary>
/// Decorator around <see cref="BedrockService"/> that records a telemetry row
/// for every Bedrock vision call. The feature tag is read from
/// <see cref="IAiTelemetryContext"/> which is set by the calling handler.
/// </summary>
public sealed class InstrumentedBedrockService(
    BedrockService          inner,
    IAiTelemetryContext     telemetryContext,
    IAiRequestLogWriter     logWriter) : IBedrockService
{
    private const string ModelName = "qwen.qwen3-vl-235b-a22b";

    public async Task<T?> AnalyzeAsync<T>(
        string base64Image,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var feature = telemetryContext.Feature ?? AiFeature.GeneralDocOcr;
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await inner.AnalyzeAsync<T>(base64Image, prompt, cancellationToken);
            sw.Stop();

            await logWriter.WriteAsync(
                feature, ModelName, success: true, (int)sw.ElapsedMilliseconds,
                userId:         telemetryContext.UserId,
                conversationId: telemetryContext.ConversationId);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();

            await logWriter.WriteAsync(
                feature, ModelName, success: false, (int)sw.ElapsedMilliseconds,
                userId:         telemetryContext.UserId,
                errorType:      ex.GetType().Name,
                conversationId: telemetryContext.ConversationId);

            throw;
        }
    }
}
