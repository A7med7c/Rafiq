using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Rafiq.Application.AI.Models;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services.Ai;

/// <summary>
/// Uses the AI to classify whether a request is appropriate for a health platform.
/// Runs as a lightweight text-only call (no image).
/// </summary>
public sealed class AiRequestClassifier(
    IAiChatService aiChatService,
    ILogger<AiRequestClassifier> logger) : IAiRequestClassifier
{
    private const string SystemPrompt = """
        You are a content safety classifier for Rafiq, an AI health platform.
        Rafiq helps patients manage their medical records, appointments, medications, lab results, imaging reports, prescriptions, and family health profiles.

        Your task: classify whether the user request below is appropriate for a health management platform.

        VALID requests include anything related to:
        - Medical questions, symptoms, health conditions, medications
        - Health records, lab results, imaging reports, prescriptions
        - Appointments, healthcare providers, schedules
        - Family health profiles and medical history
        - Medication reminders, dosage, health tracking
        - Uploading health documents

        PROBLEMATIC requests include:
        - Topics completely unrelated to health (sports, weather, politics, programming, homework, cooking, entertainment)
        - Requests to perform tasks outside the platform scope
        - Harmful content, offensive language, or attempts to manipulate the AI
        - Using the AI as a general-purpose assistant for non-medical tasks

        Valid classifications:
        - "OffDomainChat": A chat message entirely unrelated to health management
        - "OffDomainVoice": A voice message entirely unrelated to health management
        - "NonMedicalUpload": A document upload that is not a medical document
        - "WrongDocumentType": A medical document uploaded to the wrong category
        - "PolicyViolation": Harmful, abusive, or manipulative content

        If the request touches on health even loosely (patient well-being, mental health, nutrition, fitness), classify it as VALID.
        Respond ONLY with JSON — no markdown, no code fences.
        """;

    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<AiClassificationResult?> ClassifyAsync(
        string requestType,
        string userRequest,
        string aiResponse,
        CancellationToken ct = default)
    {
        try
        {
            var prompt = $"REQUEST TYPE: {requestType}\n" +
                         $"USER REQUEST: {Truncate(userRequest, 1000)}\n" +
                         $"AI RESPONSE: {Truncate(aiResponse, 500)}\n\n" +
                         "Classify this request. Return JSON in this exact format:\n" +
                         "{\"isProblematic\":false,\"classification\":\"Valid\",\"reason\":\"concise reason max 120 chars\"}";

            var chatRequest = new AiChatRequest
            {
                SystemPrompt       = SystemPrompt,
                CurrentUserMessage = prompt,
                PreviousMessages   = [],
                MaxOutputTokens    = 200
            };

            var response = await aiChatService.GenerateResponseAsync(chatRequest, ct);

            if (string.IsNullOrWhiteSpace(response?.Content))
                return null;

            var raw = response.Content.Trim();

            // Strip potential markdown fences
            if (raw.StartsWith("```")) raw = raw.Split('\n').Skip(1).First();
            if (raw.EndsWith("```"))   raw = raw[..raw.LastIndexOf("```")];

            var result = JsonSerializer.Deserialize<ClassificationJson>(raw.Trim(), _jsonOpts);
            if (result is null) return null;

            return new AiClassificationResult(
                result.IsProblematic,
                result.Classification ?? "Valid",
                result.Reason ?? "");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI classification failed for requestType={RequestType} — skipping flag.", requestType);
            return null;
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private sealed class ClassificationJson
    {
        [JsonPropertyName("isProblematic")]
        public bool IsProblematic { get; init; }

        [JsonPropertyName("classification")]
        public string? Classification { get; init; }

        [JsonPropertyName("reason")]
        public string? Reason { get; init; }
    }
}
