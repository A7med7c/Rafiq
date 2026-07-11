using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rafiq.Infrastructure.Services;

/// <summary>
/// Generic Bedrock Vision service.
/// This class is intentionally unaware of any document type
/// (Labs, Prescriptions, Radiology, etc.).
///
/// Responsibility:
///   Base64 image + Prompt → Bedrock → JSON → Deserialise&lt;T&gt;
/// </summary>
public sealed class BedrockService : IBedrockService
{
    private readonly HttpClient _httpClient;
    private readonly BedrockSettings _settings;

    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BedrockService(HttpClient httpClient, IOptions<BedrockSettings> settings)
    {
        _httpClient = httpClient;
        _settings   = settings.Value;
    }

    public async Task<T?> AnalyzeAsync<T>(
        string base64Image,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var requestBody = new
        {
            model_id = "qwen.qwen3-vl-235b-a22b",
            messages = new[]
            {
                new
                {
                    role   = "user",
                    text   = prompt,
                    images = new[]
                    {
                        new
                        {
                            format      = "jpeg",
                            data_base64 = base64Image
                        }
                    }
                }
            },
            max_tokens = 2000
        };

        var json    = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.PostAsync(
            $"{_settings.BaseUrl}/student/multimodal-chat",
            content,
            cancellationToken);

        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
            throw new ExternalServiceException(
                "Bedrock",
                $"Request failed with status code {(int)httpResponse.StatusCode}.");

        var gatewayResponse = JsonSerializer.Deserialize<BedrockGatewayResponse>(responseBody);

        if (gatewayResponse is null || string.IsNullOrWhiteSpace(gatewayResponse.OutputText))
            return default;

        return JsonSerializer.Deserialize<T>(
            gatewayResponse.OutputText,
            CaseInsensitiveOptions);
    }
}

/// <summary>
/// Represents the raw response envelope returned by the Bedrock gateway.
/// </summary>
internal sealed class BedrockGatewayResponse
{
    [JsonPropertyName("output_text")]
    public string OutputText { get; set; } = string.Empty;
}
