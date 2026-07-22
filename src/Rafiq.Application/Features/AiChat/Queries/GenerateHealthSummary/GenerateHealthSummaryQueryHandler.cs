using MediatR;
using Rafiq.Application.AI.HealthQuery;
using Rafiq.Application.AI.Models;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;

namespace Rafiq.Application.Features.AiChat.Queries.GenerateHealthSummary;

public sealed class GenerateHealthSummaryQueryHandler(
    IHealthProfileAuthorizationService healthProfileAuthService,
    IHealthQueryContextBuilder healthQueryContextBuilder,
    IAiChatService aiChatService)
    : IRequestHandler<GenerateHealthSummaryQuery, ApiResponse<HealthSummaryDto>>
{
    private const int SummaryMaxTokens = 400;

    // FamilyOverview and MedicationReminders are not included in the health summary —
    // the summary is always for the user's own profile only.
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

    private static string BuildSummarySystemPrompt(bool isArabic) => isArabic
        ? "أنت رفيق، مساعد صحي ذكي محترف وموجز. " +
          "ستُعطى بيانات صحية لمريض. اكتب ملخصًا صحيًا موجزًا من 100 إلى 200 كلمة " +
          "يشمل فقط الأقسام التي تحتوي على بيانات: الحالة الصحية العامة، الأمراض المزمنة الرئيسية، " +
          "الحساسية النشطة، الأدوية الحالية، النتائج المخبرية الملحوظة، وتوصية عامة موجزة واحدة. " +
          "احذف أي قسم لا تتوفر له بيانات. لا تخترع أي معلومات. " +
          "اكتب بأسلوب واضح ومهني وودود موجَّه للمريض. " +
          "لا تستخدم أي تنسيق Markdown مثل ** أو ## أو - أو النقاط. اكتب نصًا عاديًا فقط. " +
          "يجب أن تكتب ردك بالكامل باللغة العربية فقط."
        : "You are Rafiq, a concise and professional AI health assistant. " +
          "You will be given a patient's health data. Generate a brief health summary " +
          "in 100–200 words covering only the sections where data is present: " +
          "overall health status, key chronic conditions, active allergies, " +
          "current medications, notable lab findings, and one brief general recommendation. " +
          "Omit any section where there is no data. Do not fabricate any information. " +
          "Write in a clear, professional, and caring tone addressed to the patient. " +
          "Do not use any Markdown formatting such as **, ##, bullet points, or dashes. Plain text only. " +
          "You must write your entire response in English only.";

    public async Task<ApiResponse<HealthSummaryDto>> Handle(
        GenerateHealthSummaryQuery request, CancellationToken cancellationToken)
    {
        await healthProfileAuthService.EnsureCanReadAsync(request.UserHealthProfileId, cancellationToken);

        var intent = new ParsedHealthQueryIntent(
            AllCategories,
            HealthQueryOperation.List,
            null,
            HealthQueryTimeframe.None);

        var healthContext = await healthQueryContextBuilder.BuildAsync(
            intent, new SingleProfileScope(request.UserHealthProfileId), cancellationToken);

        if (!HasMeaningfulData(healthContext))
            return ApiResponse<HealthSummaryDto>.SuccessResponse(new HealthSummaryDto(string.Empty, false));

        bool isArabic = request.Language.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

        string userMessage = isArabic
            ? "اكتب ملخصًا صحيًا موجزًا لهذا المريض بناءً على بياناته الصحية المتاحة."
            : "Generate a concise health summary for this patient based on their available health data.";

        var aiRequest = new AiChatRequest
        {
            SystemPrompt = BuildSummarySystemPrompt(isArabic),
            HealthContext = healthContext,
            CurrentUserMessage = userMessage,
            MaxOutputTokens = SummaryMaxTokens
        };

        var response = await aiChatService.GenerateResponseAsync(aiRequest, cancellationToken);
        var plainText = StripMarkdown(response.Content);

        return ApiResponse<HealthSummaryDto>.SuccessResponse(new HealthSummaryDto(plainText, true));
    }

    private static string StripMarkdown(string text)
    {
        // Remove bold/italic markers
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*{1,3}(.+?)\*{1,3}", "$1");
        // Remove ATX headings (## Heading)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^#{1,6}\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        // Convert bullet/dash list items to inline text with a comma separator
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^\s*[-*]\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        // Collapse multiple blank lines into one
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }

    // Real data produces list items formatted as "\n- ..." by the context builder.
    // If none are present, every category returned an "empty" message — not enough to summarize.
    private static bool HasMeaningfulData(string context) =>
        !string.IsNullOrWhiteSpace(context) && context.Contains("\n- ");
}
