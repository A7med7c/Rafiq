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

        // Build the messages array
        var messages = new List<object>();

        // Add history
        foreach (var msg in request.PreviousMessages)
        {
            messages.Add(new
            {
                role = msg.Role.ToString().ToLower(),
                content = msg.Content
            });
        }

        // Add current message
        if (hasImage)
        {
            messages.Add(new
            {
                role = "user",
                text = request.CurrentUserMessage,
                images = new[]
                {
                    new
                    {
                        format = request.ImageFormat?.ToLower() ?? "jpeg",
                        data_base64 = request.Base64Image
                    }
                }
            });
        }
        else
        {
            messages.Add(new
            {
                role = "user",
                content = request.CurrentUserMessage
            });
        }

        // The system prompt combines the AI instructions and the health context
        var systemPrompt = request.SystemPrompt;
        if (!string.IsNullOrWhiteSpace(request.HealthContext))
        {
            systemPrompt += $"\n\nContext:\n{request.HealthContext}";
        }

        var requestBody = new
        {
            model_id = modelId,
            messages = messages,
            system_prompt = systemPrompt,
            max_tokens = request.MaxOutputTokens ?? _chatSettings.MaxOutputTokens
        };

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

    private sealed class AiGatewayResponse
    {
        [JsonPropertyName("output_text")]
        public string OutputText { get; set; } = string.Empty;
    }
}
