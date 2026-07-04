namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// Generic AI vision service.
/// Receives a Base64 image and a prompt,
/// returns a deserialised result of type T.
/// This service is intentionally unaware of Labs, Prescriptions,
/// Radiology or any other document type.
/// </summary>
public interface IBedrockService
{
    Task<T?> AnalyzeAsync<T>(
        string base64Image,
        string prompt,
        CancellationToken cancellationToken = default);
}
