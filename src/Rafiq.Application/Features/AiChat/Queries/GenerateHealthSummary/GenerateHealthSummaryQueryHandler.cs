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

    private static readonly IReadOnlyList<HealthQueryCategory> AllCategories =
        Enum.GetValues<HealthQueryCategory>().ToArray();

    private const string SummarySystemPrompt =
        "You are Rafiq, a concise and professional AI health assistant. " +
        "You will be given a patient's health data. Generate a brief health summary " +
        "in 100–200 words covering only the sections where data is present: " +
        "overall health status, key chronic conditions, active allergies, " +
        "current medications, notable lab findings, and one brief general recommendation. " +
        "Omit any section where there is no data. Do not fabricate any information. " +
        "Write in a clear, professional, and caring tone addressed to the patient.";

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
            intent, request.UserHealthProfileId, cancellationToken);

        if (!HasMeaningfulData(healthContext))
            return ApiResponse<HealthSummaryDto>.SuccessResponse(new HealthSummaryDto(string.Empty, false));

        var aiRequest = new AiChatRequest
        {
            SystemPrompt = SummarySystemPrompt,
            HealthContext = healthContext,
            CurrentUserMessage = "Generate a concise health summary for this patient based on their available health data.",
            MaxOutputTokens = SummaryMaxTokens
        };

        var response = await aiChatService.GenerateResponseAsync(aiRequest, cancellationToken);

        return ApiResponse<HealthSummaryDto>.SuccessResponse(new HealthSummaryDto(response.Content, true));
    }

    // Real data produces list items formatted as "\n- ..." by the context builder.
    // If none are present, every category returned an "empty" message — not enough to summarize.
    private static bool HasMeaningfulData(string context) =>
        !string.IsNullOrWhiteSpace(context) && context.Contains("\n- ");
}
