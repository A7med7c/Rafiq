using System.Text.Json;
using MediatR;
using Rafiq.Application.AI.Models;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Queries.CheckMedicationSafety;

public sealed class CheckMedicationSafetyQueryHandler(
    IAllergyRepository allergyRepository,
    IAiChatService aiChatService)
    : IRequestHandler<CheckMedicationSafetyQuery, ApiResponse<AllergyCheckResultDto>>
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private static readonly HashSet<string> HighRiskLevels =
        new(StringComparer.OrdinalIgnoreCase) { "High", "Medium" };

    public async Task<ApiResponse<AllergyCheckResultDto>> Handle(
        CheckMedicationSafetyQuery request,
        CancellationToken cancellationToken)
    {
        var allergies = await allergyRepository.GetAllByProfileIdAsync(
            request.ProfileId, cancellationToken);

        if (allergies.Count == 0)
            return Safe();

        var allergyList = string.Join(", ",
            allergies.Select(a => $"{a.Name} (severity: {a.Severity})"));

        var prompt = $$"""
            You are a strict clinical pharmacist. Your job is to detect REAL, CLINICALLY ESTABLISHED allergy risks only.

            Patient recorded allergies: {{allergyList}}
            Medication to evaluate: "{{request.MedicationName}}"

            Rules:
            - Return isSafe=true and riskLevel="None" unless there is a DIRECT, ESTABLISHED cross-reaction or contraindication between the medication and one of the listed allergies.
            - A medication that is completely unrelated to any listed allergy MUST be marked isSafe=true.
            - Random words, food names, or clearly non-medical text that are not real drug names MUST be marked isSafe=true.
            - Only set isSafe=false if the medication shares an active ingredient, drug class, or has a well-documented cross-reactivity with one of the listed allergies.
            - riskLevel values: "None" (no risk), "Low" (minor/rare), "Medium" (moderate, consult doctor), "High" (strong contraindication).
            - Be conservative: when in doubt, return isSafe=true.

            Respond ONLY with this JSON (no markdown, no extra text):
            {"isSafe":true,"riskLevel":"None","triggeredAllergy":null,"explanation":null}
            """;

        var aiRequest = new AiChatRequest
        {
            SystemPrompt = "You are a clinical pharmacist safety checker. Output ONLY a single JSON object. No markdown. No explanation outside the JSON.",
            CurrentUserMessage = prompt,
            MaxOutputTokens = 250,
        };

        try
        {
            var aiResponse = await aiChatService.GenerateResponseAsync(aiRequest, cancellationToken);
            var content = ExtractJson(aiResponse.Content);

            var result = JsonSerializer.Deserialize<AiSafetyResponse>(content, JsonOpts);
            if (result is null) return Safe();

            // Only treat as unsafe when the AI reports Medium or High risk
            var riskLevel = result.RiskLevel ?? "None";
            var isRealRisk = !result.IsSafe && HighRiskLevels.Contains(riskLevel);

            return ApiResponse<AllergyCheckResultDto>.SuccessResponse(
                new AllergyCheckResultDto(
                    !isRealRisk,
                    riskLevel,
                    isRealRisk ? result.TriggeredAllergy : null,
                    isRealRisk ? result.Explanation : null),
                "Safety check complete.");
        }
        catch
        {
            return Safe();
        }
    }

    private static string ExtractJson(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith('{')) return trimmed;

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return (start >= 0 && end > start) ? trimmed[start..(end + 1)] : trimmed;
    }

    private static ApiResponse<AllergyCheckResultDto> Safe() =>
        ApiResponse<AllergyCheckResultDto>.SuccessResponse(
            new AllergyCheckResultDto(true, "None", null, null),
            "Safety check complete.");
}

file sealed class AiSafetyResponse
{
    public bool IsSafe { get; init; } = true;
    public string? RiskLevel { get; init; }
    public string? TriggeredAllergy { get; init; }
    public string? Explanation { get; init; }
}
