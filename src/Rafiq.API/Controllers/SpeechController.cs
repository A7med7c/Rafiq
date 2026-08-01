using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rafiq.API.Controllers;

/// <summary>Request body for the ElevenLabs TTS proxy endpoint.</summary>
public sealed record ElevenLabsTtsRequest(string Text);

[ApiController]
[AllowAnonymous]
[Route("api/speech")]
public class SpeechController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public SpeechController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Issues a temporary Azure Speech token for authenticated clients.
    /// Obtains a short-lived token from Azure Speech REST API without exposing the subscription key to clients.
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> GetToken(CancellationToken cancellationToken)
    {
        var key = _configuration["AzureSpeech:Key"];
        var region = _configuration["AzureSpeech:Region"];

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(region) ||
            key == "YOUR_AZURE_SPEECH_KEY" || region == "YOUR_AZURE_SPEECH_REGION")
        {
            return BadRequest(new
            {
                message = "Azure Speech key or region is not properly configured in appsettings.json."
            });
        }

        var issueTokenUrl = $"https://{region}.api.cognitive.microsoft.com/sts/v1.0/issueToken";

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, issueTokenUrl);
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken);
                return StatusCode((int)response.StatusCode, new
                {
                    message = "Failed to issue Azure Speech token.",
                    details = errorDetails
                });
            }

            var token = await response.Content.ReadAsStringAsync(cancellationToken);

            return Ok(new
            {
                token,
                region,
                expiresInSeconds = 600
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An error occurred while calling the Azure Speech Service token endpoint.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Proxies text-to-speech through ElevenLabs so the API key never reaches the browser.
    /// Returns an MP3 audio stream. Responds 404 when ElevenLabs is not configured,
    /// which the frontend treats as "fall back to Azure TTS".
    /// </summary>
    [HttpPost("elevenlabs-tts")]
    public async Task<IActionResult> ElevenLabsTts([FromBody] ElevenLabsTtsRequest request, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["ElevenLabs:ApiKey"];
        var voiceId = _configuration["ElevenLabs:VoiceId"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(voiceId) ||
            apiKey == "YOUR_ELEVENLABS_API_KEY" || voiceId == "YOUR_ELEVENLABS_VOICE_ID")
        {
            return NotFound(new { message = "ElevenLabs is not configured in appsettings.json." });
        }

        if (string.IsNullOrWhiteSpace(request?.Text))
        {
            return BadRequest(new { message = "Text is required." });
        }

        if (request.Text.Length > 2000)
        {
            return BadRequest(new { message = "Text must be under 2000 characters." });
        }

        var modelId = _configuration["ElevenLabs:ModelId"];
        if (string.IsNullOrWhiteSpace(modelId))
        {
            // Multilingual v2 has the best Arabic quality; auto-detects language from text.
            modelId = "eleven_multilingual_v2";
        }

        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}?output_format=mp3_44100_128";

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("xi-api-key", apiKey);
            httpRequest.Content = JsonContent.Create(new
            {
                text = request.Text,
                model_id = modelId,
                voice_settings = new
                {
                    stability = 0.5,
                    similarity_boost = 0.75,
                    style = 0.0,
                    use_speaker_boost = true
                }
            });

            var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync(cancellationToken);
                return StatusCode((int)response.StatusCode, new
                {
                    message = "ElevenLabs TTS request failed.",
                    details
                });
            }

            var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return File(audioBytes, "audio/mpeg");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An error occurred while calling the ElevenLabs TTS endpoint.",
                error = ex.Message
            });
        }
    }
}
