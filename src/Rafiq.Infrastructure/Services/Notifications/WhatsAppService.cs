using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services.Notifications;

public sealed class WhatsAppService(
    HttpClient httpClient,
    IOptions<WhatsAppSettings> options,
    ILogger<WhatsAppService> logger) : IWhatsAppService
{
    private readonly WhatsAppSettings _settings = options.Value;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task SendTemplateAsync(
        string recipientPhoneNumber,
        string templateName,
        List<string> bodyParameters,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = NormalizePhoneNumber(recipientPhoneNumber);

        var payload = new
        {
            messaging_product = "whatsapp",
            to = normalizedPhone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = "ar_EG" },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = bodyParameters
                            .Select(p => new { type = "text", text = p })
                            .ToArray()
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://graph.facebook.com/v21.0/{_settings.PhoneNumberId}/messages");

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.AccessToken);

        request.Content = JsonContent.Create(payload, options: _jsonOptions);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "WhatsApp API returned {StatusCode} for template '{Template}' to '{Phone}'. Response: {Body}",
                (int)response.StatusCode, templateName, normalizedPhone, body);
            return;
        }

        logger.LogInformation(
            "WhatsApp template '{Template}' sent to '{Phone}'.", templateName, normalizedPhone);
    }

    // Normalises any Egyptian phone number format to the international digits-only format
    // expected by the Graph API (e.g. "01012345678" → "201012345678").
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        phoneNumber = phoneNumber.Trim().Replace(" ", "").Replace("-", "");

        // Strip leading + so the API receives digits only.
        if (phoneNumber.StartsWith('+'))
            return phoneNumber[1..];

        // Already has country code without +.
        if (phoneNumber.StartsWith("20"))
            return phoneNumber;

        // Local Egyptian format starting with 0 (e.g. 01012345678).
        if (phoneNumber.StartsWith('0'))
            return $"2{phoneNumber}";

        // Fallback: prepend Egypt country code.
        return $"20{phoneNumber}";
    }
}
