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
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task SendTemplateAsync(
        string recipientPhoneNumber,
        string templateName,
        List<string> bodyParameters,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = NormalizePhoneNumber(recipientPhoneNumber);

        // Try primary language code "ar_EG", fallback to "ar" if Meta rejects language code
        var success = await SendTemplateWithLanguageAsync(normalizedPhone, templateName, bodyParameters, "ar_EG", cancellationToken);
        if (!success)
        {
            logger.LogInformation("[WhatsApp] Retrying template '{Template}' to '{Phone}' with language code 'ar'...", templateName, normalizedPhone);
            await SendTemplateWithLanguageAsync(normalizedPhone, templateName, bodyParameters, "ar", cancellationToken);
        }

        // Add 300ms delay to prevent Meta Cloud API from dropping/rate-limiting consecutive rapid requests
        await Task.Delay(300, cancellationToken);
    }

    private async Task<bool> SendTemplateWithLanguageAsync(
        string normalizedPhone,
        string templateName,
        List<string> bodyParameters,
        string languageCode,
        CancellationToken cancellationToken)
    {
        object? components = null;

        if (bodyParameters.Count > 0)
        {
            components = new[]
            {
                new
                {
                    type = "body",
                    parameters = bodyParameters
                        .Select(p => new { type = "text", text = p })
                        .ToArray()
                }
            };
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = normalizedPhone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = languageCode },
                components
            }
        };

        var requestJson = JsonSerializer.Serialize(payload, _jsonOptions);
        logger.LogInformation(
            "[WhatsApp] POST Template → graph.facebook.com | template='{Template}' | lang='{Lang}' | to='{Phone}' | params=[{Params}] | body={Body}",
            templateName,
            languageCode,
            normalizedPhone,
            string.Join(", ", bodyParameters),
            requestJson);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://graph.facebook.com/v21.0/{_settings.PhoneNumberId}/messages");

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.AccessToken);

        request.Content = JsonContent.Create(payload, options: _jsonOptions);

        var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "[WhatsApp] FAILED Template {StatusCode} | template='{Template}' | lang='{Lang}' | to='{Phone}' | response={Body}",
                (int)response.StatusCode, templateName, languageCode, normalizedPhone, responseBody);
            return false;
        }

        logger.LogInformation(
            "[WhatsApp] SUCCESS Template | template='{Template}' | to='{Phone}' | response={Body}",
            templateName, normalizedPhone, responseBody);

        return true;
    }

    public async Task SendTextMessageAsync(
        string recipientPhoneNumber,
        string messageText,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = NormalizePhoneNumber(recipientPhoneNumber);

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = normalizedPhone,
            type = "text",
            text = new
            {
                preview_url = false,
                body = messageText
            }
        };

        var requestJson = JsonSerializer.Serialize(payload, _jsonOptions);
        logger.LogInformation(
            "[WhatsApp] POST Text → graph.facebook.com | to='{Phone}' | message='{Message}' | body={Body}",
            normalizedPhone, messageText, requestJson);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://graph.facebook.com/v21.0/{_settings.PhoneNumberId}/messages");

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.AccessToken);

        request.Content = JsonContent.Create(payload, options: _jsonOptions);

        var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "[WhatsApp] FAILED Text {StatusCode} | to='{Phone}' | response={Body}",
                (int)response.StatusCode, normalizedPhone, responseBody);
            return;
        }

        logger.LogInformation(
            "[WhatsApp] SUCCESS Text | to='{Phone}' | response={Body}",
            normalizedPhone, responseBody);
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
