using System.Text.Json;
using MediatR;
using Rafiq.Application.AI.HealthQuery;
using Rafiq.Application.AI.Models;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.AiChat.Queries.GenerateHealthSummary;

public sealed class GenerateHealthSummaryQueryHandler(
    IHealthProfileAuthorizationService healthProfileAuthService,
    IHealthQueryContextBuilder healthQueryContextBuilder,
    IAiChatService aiChatService,
    IHealthSummaryCacheRepository summaryCache)
    : IRequestHandler<GenerateHealthSummaryQuery, ApiResponse<HealthSummaryDto>>
{
    private const int SummaryMaxTokens = 700;

    private static readonly HealthSummaryDto EmptyDto = new(
        "Good", null, [], [], new(0, false, null), new("Normal", 0), [], [], false);

    private static readonly IReadOnlyList<HealthQueryCategory> AllCategories =
    [
        HealthQueryCategory.Profile,
        HealthQueryCategory.Allergies,
        HealthQueryCategory.ChronicDiseases,
        HealthQueryCategory.Medicines,
        HealthQueryCategory.Appointments,
        HealthQueryCategory.LabReports,
        HealthQueryCategory.Prescriptions,
        HealthQueryCategory.ImagingReports
    ];

    private static string BuildSummarySystemPrompt(bool isArabic)
    {
        var langNote = isArabic
            ? "All text values (conditions, allergy names, overallStatusNote, insights, recommendations) must be written in Arabic. " +
              "The enum values overallStatus, labResults.status, and allergies[].severity must always be the exact English strings specified below."
            : "All values must be written in English.";

        return
            "You are Rafiq, an AI health assistant. " +
            "Analyze the patient's health data and return ONLY a valid JSON object — no markdown, no code blocks, no explanation, no extra text before or after.\n\n" +
            "Return this exact schema:\n" +
            "{\n" +
            "  \"overallStatus\": \"Good\" | \"Stable\" | \"Needs Attention\",\n" +
            "  \"overallStatusNote\": \"One very brief phrase (max 8 words) describing why\",\n" +
            "  \"conditions\": [\"active condition name\", ...],\n" +
            "  \"allergies\": [{\"name\": \"allergen\", \"severity\": \"Mild\" | \"Moderate\" | \"Severe\"}, ...],\n" +
            "  \"medications\": {\"count\": N, \"hasIssues\": false, \"issueNote\": null},\n" +
            "  \"labResults\": {\"status\": \"Normal\" | \"HasAbnormal\" | \"ReviewRecommended\", \"abnormalCount\": N},\n" +
            "  \"insights\": [\"One sentence.\", ...],\n" +
            "  \"recommendations\": [\"One actionable sentence.\", ...]\n" +
            "}\n\n" +
            "Rules:\n" +
            "- overallStatus must be exactly one of: \"Good\", \"Stable\", \"Needs Attention\".\n" +
            "- conditions: up to 3 active/ongoing conditions. Empty array if none.\n" +
            "- allergies: up to 3 allergies. Empty array if none. severity must be \"Mild\", \"Moderate\", or \"Severe\".\n" +
            "- medications.count: total number of current medications (0 if none).\n" +
            "- medications.hasIssues: true only if duplicate medications or potential drug conflicts are detected.\n" +
            "- labResults.status: \"Normal\" if all values in range, \"HasAbnormal\" if any out-of-range values exist, \"ReviewRecommended\" if follow-up is suggested.\n" +
            "- labResults.abnormalCount: count of out-of-range lab values (0 if none).\n" +
            "- insights: 2 to 4 items. Each must be exactly one sentence.\n" +
            "- recommendations: 2 to 3 items. Each must be exactly one actionable sentence.\n" +
            "- Do NOT invent information not present in the patient records.\n" +
            "- Do NOT list every item — include only the most clinically relevant.\n" +
            langNote + "\n" +
            "Return ONLY the JSON object. Nothing else.";
    }

    public async Task<ApiResponse<HealthSummaryDto>> Handle(
        GenerateHealthSummaryQuery request, CancellationToken cancellationToken)
    {
        await healthProfileAuthService.EnsureCanReadAsync(request.UserHealthProfileId, cancellationToken);

        var lang = request.Language.ToLowerInvariant().StartsWith("ar") ? "ar" : "en";

        // Return the cached summary when it is still valid.
        var cached = await summaryCache.GetAsync(request.UserHealthProfileId, lang, cancellationToken);
        if (cached is not null && !cached.NeedsRefresh)
        {
            var cachedDto = DeserializeSummary(cached.SummaryJson);
            if (cachedDto is not null)
                return ApiResponse<HealthSummaryDto>.SuccessResponse(cachedDto);
        }

        // Cache is missing or stale — generate a fresh summary.
        var intent = new ParsedHealthQueryIntent(
            AllCategories,
            HealthQueryOperation.List,
            null,
            HealthQueryTimeframe.None);

        var healthContext = await healthQueryContextBuilder.BuildAsync(
            intent, new SingleProfileScope(request.UserHealthProfileId), cancellationToken);

        if (!HasMeaningfulData(healthContext))
        {
            // No meaningful data — persist the empty result to avoid re-calling AI on every refresh.
            var emptyJson = SerializeSummary(EmptyDto);
            var emptyEntry = cached is null
                ? HealthSummaryCache.Create(request.UserHealthProfileId, lang, emptyJson)
                : cached;
            if (cached is not null) cached.Refresh(emptyJson);
            await summaryCache.SaveAsync(emptyEntry, cancellationToken);
            return ApiResponse<HealthSummaryDto>.SuccessResponse(EmptyDto);
        }

        bool isArabic = lang == "ar";

        string userMessage = isArabic
            ? "حلل بيانات المريض وأنشئ ملخص الصحة بتنسيق JSON المطلوب."
            : "Analyze the patient data and generate the health summary in the required JSON format.";

        var aiRequest = new AiChatRequest
        {
            SystemPrompt = BuildSummarySystemPrompt(isArabic),
            HealthContext = healthContext,
            CurrentUserMessage = userMessage,
            MaxOutputTokens = SummaryMaxTokens
        };

        var response = await aiChatService.GenerateResponseAsync(aiRequest, cancellationToken);
        var dto = ParseSummaryJson(response.Content, isArabic);

        // Persist to cache.
        var summaryJson = SerializeSummary(dto);
        var entry = cached is null
            ? HealthSummaryCache.Create(request.UserHealthProfileId, lang, summaryJson)
            : cached;
        if (cached is not null) cached.Refresh(summaryJson);
        await summaryCache.SaveAsync(entry, cancellationToken);

        return ApiResponse<HealthSummaryDto>.SuccessResponse(dto);
    }

    private static string SerializeSummary(HealthSummaryDto dto)
        => JsonSerializer.Serialize(dto);

    private static HealthSummaryDto? DeserializeSummary(string json)
    {
        try { return JsonSerializer.Deserialize<HealthSummaryDto>(json); }
        catch { return null; }
    }

    private static HealthSummaryDto ParseSummaryJson(string raw, bool isArabic)
    {
        try
        {
            // Strip any accidental markdown code fence the model might add
            var json = raw.Trim();
            if (json.StartsWith("```"))
            {
                var start = json.IndexOf('\n') + 1;
                var end   = json.LastIndexOf("```");
                if (end > start) json = json[start..end].Trim();
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var overallStatus = root.TryGetProperty("overallStatus", out var os)
                ? NormaliseStatus(os.GetString())
                : "Stable";

            var statusNote = root.TryGetProperty("overallStatusNote", out var osn)
                ? osn.GetString()
                : null;

            var conditions = ReadStringArray(root, "conditions", 3);
            var allergies  = ReadAllergies(root);

            var medCount    = 0;
            var medIssues   = false;
            string? medNote = null;
            if (root.TryGetProperty("medications", out var meds))
            {
                if (meds.TryGetProperty("count",     out var mc)) medCount  = mc.GetInt32();
                if (meds.TryGetProperty("hasIssues", out var mi)) medIssues = mi.GetBoolean();
                if (meds.TryGetProperty("issueNote", out var mn) && mn.ValueKind == JsonValueKind.String)
                    medNote = mn.GetString();
            }

            var labStatus       = "Normal";
            var labAbnormalCount = 0;
            if (root.TryGetProperty("labResults", out var lab))
            {
                if (lab.TryGetProperty("status",        out var ls)) labStatus        = NormaliseLabStatus(ls.GetString());
                if (lab.TryGetProperty("abnormalCount",  out var lc)) labAbnormalCount = lc.GetInt32();
            }

            var insights        = ReadStringArray(root, "insights",        4);
            var recommendations = ReadStringArray(root, "recommendations", 3);

            return new HealthSummaryDto(
                overallStatus, statusNote, conditions, allergies,
                new(medCount, medIssues, medNote),
                new(labStatus, labAbnormalCount),
                insights, recommendations,
                HasData: true);
        }
        catch
        {
            // If JSON parsing fails for any reason, return a safe fallback
            return new HealthSummaryDto("Stable", null, [], [], new(0, false, null), new("Normal", 0), [], [], true);
        }
    }

    private static string NormaliseStatus(string? s) => s switch
    {
        "Good"            => "Good",
        "Stable"          => "Stable",
        "Needs Attention" => "Needs Attention",
        _                 => "Stable"
    };

    private static string NormaliseLabStatus(string? s) => s switch
    {
        "Normal"               => "Normal",
        "HasAbnormal"          => "HasAbnormal",
        "ReviewRecommended"    => "ReviewRecommended",
        _                      => "Normal"
    };

    private static List<string> ReadStringArray(JsonElement root, string key, int max)
    {
        if (!root.TryGetProperty(key, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];
        return arr.EnumerateArray()
            .Take(max)
            .Select(e => e.GetString() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static List<AllergyBriefDto> ReadAllergies(JsonElement root)
    {
        if (!root.TryGetProperty("allergies", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];
        return arr.EnumerateArray()
            .Take(3)
            .Select(e =>
            {
                var name     = e.TryGetProperty("name",     out var n) ? n.GetString() ?? "" : "";
                var severity = e.TryGetProperty("severity", out var s) ? s.GetString() ?? "" : "";
                return new AllergyBriefDto(name, severity);
            })
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .ToList();
    }

    private static bool HasMeaningfulData(string context) =>
        !string.IsNullOrWhiteSpace(context) && context.Contains("\n- ");
}
