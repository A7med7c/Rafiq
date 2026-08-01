namespace Rafiq.Application.Common.Interfaces;

public sealed record AiClassificationResult(
    bool IsProblematic,
    string Classification,
    string Reason);

/// <summary>
/// Calls the AI to classify whether a given request is appropriate for a health platform.
/// Returns null if the classification call itself fails (non-fatal).
/// </summary>
public interface IAiRequestClassifier
{
    Task<AiClassificationResult?> ClassifyAsync(
        string requestType,
        string userRequest,
        string aiResponse,
        CancellationToken ct = default);
}
