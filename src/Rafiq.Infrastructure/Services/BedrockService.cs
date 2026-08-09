using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
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

    public BedrockService(HttpClient httpClient, IOptions<BedrockSettings> settings, ILogger<BedrockService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _settings   = settings.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private readonly ILogger<BedrockService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<T?> AnalyzeAsync<T>(
        string base64Image,
        string prompt,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var modelId = "qwen.qwen3-vl-235b-a22b";

        var strippedBase64 = StripBase64Prefix(base64Image);

        // Build the combined prompt since the multimodal model does not support system messages
        var combinedPrompt = string.IsNullOrWhiteSpace(systemPrompt) 
            ? prompt 
            : $"{systemPrompt}\n\n{prompt}";

        // Build the messages list — only user message
        var userMessage = new
        {
            role   = "user",
            text   = combinedPrompt,
            images = new[]
            {
                new
                {
                    format      = "jpeg",
                    data_base64 = strippedBase64
                }
            }
        };

        var requestBody = new
        {
            model_id   = modelId,
            messages   = new object[] { userMessage },
            max_tokens = 4000
        };

        var json    = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUrl = $"{_settings.BaseUrl}/student/multimodal-chat";
        var httpResponse = await _httpClient.PostAsync(
            requestUrl,
            content,
            cancellationToken);

        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var requestBodyForLog = new
            {
                model_id = modelId,
                messages = new object[] { new { role = "user", text = prompt, images = new[] { new { format = "jpeg", data_base64 = "[BASE64_IMAGE_HIDDEN]" } } } },
                max_tokens = 2000
            };
            var correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? "N/A";
            
            _logger.LogError(
                "Bedrock API Call Failed.\n" +
                "Correlation Id: {CorrelationId}\n" +
                "Request URL: {RequestUrl}\n" +
                "HTTP Method: POST\n" +
                "Response Status Code: {StatusCode}\n" +
                "Response Body: {ResponseBody}\n" +
                "Request Body: {RequestBody}\n" +
                "Model Id: {ModelId}\n" +
                "BaseUrl: {BaseUrl}\n" +
                "Timeout: {Timeout}",
                correlationId,
                requestUrl,
                httpResponse.StatusCode,
                responseBody,
                JsonSerializer.Serialize(requestBodyForLog),
                modelId,
                _settings.BaseUrl,
                _httpClient.Timeout);

            throw new ExternalServiceException(
                "Bedrock",
                $"Request failed with status code {(int)httpResponse.StatusCode}. Details: {responseBody}");
        }

        var gatewayResponse = JsonSerializer.Deserialize<BedrockGatewayResponse>(responseBody);

        if (gatewayResponse is null || string.IsNullOrWhiteSpace(gatewayResponse.OutputText))
            return default;

        _logger.LogInformation("RAW BEDROCK JSON RESPONSE: {Response}", gatewayResponse.OutputText);

        return JsonSerializer.Deserialize<T>(
            gatewayResponse.OutputText,
            CaseInsensitiveOptions);
    }
    private static string StripBase64Prefix(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return base64;
        
        if (!base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return base64;
        }

        var commaIndex = base64.IndexOf(',');
        return commaIndex >= 0 ? base64[(commaIndex + 1)..] : base64;
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
