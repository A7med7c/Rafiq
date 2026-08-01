using Hangfire;
using Microsoft.Extensions.Logging;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Fire-and-forget Hangfire job that classifies an AI request for policy compliance.
/// Runs after the main AI call completes — never blocks the user.
/// </summary>
public sealed class AiRequestClassificationJob(
    IAiRequestClassifier classifier,
    IUsageIntelligenceService usageIntelligence,
    ILogger<AiRequestClassificationJob> logger)
{
    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(
        Guid   userId,
        string requestType,
        string userRequest,
        string aiResponse)
    {
        logger.LogDebug(
            "AiRequestClassificationJob started for userId={UserId} requestType={RequestType}",
            userId, requestType);

        var result = await classifier.ClassifyAsync(requestType, userRequest, aiResponse);

        if (result is null)
        {
            logger.LogDebug("Classification returned null (classifier error) — skipping.");
            return;
        }

        if (!result.IsProblematic)
        {
            logger.LogDebug("Request classified as valid — no flag created.");
            return;
        }

        logger.LogInformation(
            "Flagging AI request: userId={UserId}, classification={Class}, reason={Reason}",
            userId, result.Classification, result.Reason);

        await usageIntelligence.SaveFlaggedRequestAsync(new(
            UserId:         userId,
            RequestType:    requestType,
            UserRequest:    userRequest,
            AiResponse:     aiResponse,
            Classification: result.Classification,
            Reason:         result.Reason));
    }
}
