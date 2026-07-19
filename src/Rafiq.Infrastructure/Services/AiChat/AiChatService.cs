using Microsoft.Extensions.Options;
using Rafiq.Application.AI.Models;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rafiq.Infrastructure.Services.AiChat;

public sealed class AiChatService : IAiChatService
{
    private const string DefaultImageCaption = "Please describe what you see in this image.";
    private static readonly string[] SupportedImageFormats = { "jpeg", "png", "webp" };

    private const string MedicalImageGuardPrompt =
        "SYSTEM INSTRUCTION (highest priority — follow exactly):\n" +
        "You are Rafiq, an AI healthcare assistant. Your role is strictly limited to healthcare.\n\n" +
        "Step 1 — Classify the image:\n" +
        "Determine whether the image is medical or healthcare-related. Medical images include:\n" +
        "prescriptions, lab reports, blood tests, X-rays, MRI, CT scans, ultrasounds, medication boxes,\n" +
        "medical documents, skin conditions, wounds, injuries, rashes, or any other clinical content.\n\n" +
        "Step 2 — Decide:\n" +
        "• If the image IS medical: analyze it and respond helpfully in the user's language.\n" +
        "• If the image is NOT medical (e.g. person, landscape, animal, car, food, etc.):\n" +
        "  Do NOT describe or analyze it. Respond ONLY with this message (translated to the user's language if needed):\n" +
        "  \"I'm Rafiq, your healthcare assistant. I can only analyze medical images such as prescriptions, " +
        "lab reports, medical scans, medications, and other healthcare-related documents. " +
        "Please upload a medical image if you need assistance.\"\n\n" +
        "User message: ";

    private readonly HttpClient _httpClient;
    private readonly BedrockSettings _bedrockSettings;
    private readonly AiChatSettings _chatSettings;

    public AiChatService(
        HttpClient httpClient,
        IOptions<BedrockSettings> bedrockSettings,
        IOptions<AiChatSettings> chatSettings)
    {
        _httpClient = httpClient;
        _bedrockSettings = bedrockSettings.Value;
        _chatSettings = chatSettings.Value;
    }

    public async Task<AiChatResponse> GenerateResponseAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _bedrockSettings.ApiKey);

        var hasImage = !string.IsNullOrWhiteSpace(request.Base64Image);

        var endpoint = hasImage ? _chatSettings.MultimodalEndpointPath : _chatSettings.EndpointPath;
        var modelId = hasImage ? _chatSettings.MultimodalModelId : _chatSettings.ModelId;

        object requestBody;

        if (hasImage)
        {
            // The multimodal gateway uses a different message schema than the text-chat
            // gateway: "text" instead of "content", plus an "images" array, and no
            // "system_prompt" field at all.
            var messages = new List<object>();

            foreach (var msg in request.PreviousMessages)
            {
                messages.Add(new
                {
                    role = msg.Role.ToString().ToLower(),
                    text = msg.Content
                });
            }

            var userText = string.IsNullOrWhiteSpace(request.CurrentUserMessage)
                ? DefaultImageCaption
                : request.CurrentUserMessage;

            var imageText = MedicalImageGuardPrompt + userText;

            messages.Add(new
            {
                role = "user",
                text = imageText,
                images = new[]
                {
                    new
                    {
                        format = NormalizeImageFormat(request.ImageFormat),
                        data_base64 = StripBase64Prefix(request.Base64Image!)
                    }
                }
            });

            requestBody = new
            {
                model_id = modelId,
                messages = messages,
                max_tokens = request.MaxOutputTokens ?? _chatSettings.MaxOutputTokens
            };
        }
        else
        {
            var messages = new List<object>();

            foreach (var msg in request.PreviousMessages)
            {
                messages.Add(new
                {
                    role = msg.Role.ToString().ToLower(),
                    content = msg.Content
                });
            }

            messages.Add(new
            {
                role = "user",
                content = request.CurrentUserMessage
            });

            // The system prompt combines the AI instructions and the health context
            var systemPrompt = request.SystemPrompt;
            if (!string.IsNullOrWhiteSpace(request.HealthContext))
            {
                systemPrompt += $"\n\nContext:\n{request.HealthContext}";
            }

            requestBody = new
            {
                model_id = modelId,
                messages = messages,
                system_prompt = systemPrompt,
                max_tokens = request.MaxOutputTokens ?? _chatSettings.MaxOutputTokens
            };
        }

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.PostAsync(
            $"{_bedrockSettings.BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}",
            content,
            cancellationToken);

        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
            throw new ExternalServiceException("AiChat", $"Request failed with status code {(int)httpResponse.StatusCode}. Details: {responseBody}");

        var gatewayResponse = JsonSerializer.Deserialize<AiGatewayResponse>(responseBody);

        return new AiChatResponse
        {
            Content = gatewayResponse?.OutputText ?? string.Empty
        };
    }

    /// <summary>
    /// The gateway only accepts "jpeg", "png" or "webp" — strips MIME-type prefixes
    /// (e.g. "image/jpeg") and normalizes "jpg" to "jpeg" defensively, in case a caller
    /// sends a MIME type instead of the short format string.
    /// </summary>
    private static string NormalizeImageFormat(string? format)
    {
        var normalized = (format ?? "jpeg").Trim().ToLowerInvariant();

        const string mimePrefix = "image/";
        if (normalized.StartsWith(mimePrefix, StringComparison.Ordinal))
        {
            normalized = normalized[mimePrefix.Length..];
        }

        if (normalized == "jpg")
        {
            normalized = "jpeg";
        }

        return Array.IndexOf(SupportedImageFormats, normalized) >= 0 ? normalized : "jpeg";
    }

    /// <summary>
    /// Defensively strips a "data:image/...;base64," prefix if present, since the
    /// gateway expects data_base64 to contain only the raw Base64 payload.
    /// </summary>
    private static string StripBase64Prefix(string base64)
    {
        if (!base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return base64;
        }

        var commaIndex = base64.IndexOf(',');
        return commaIndex >= 0 ? base64[(commaIndex + 1)..] : base64;
    }

    private sealed class AiGatewayResponse
    {
        [JsonPropertyName("output_text")]
        public string OutputText { get; set; } = string.Empty;
    }
}
