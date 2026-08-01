using Hangfire;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services.BackgroundJobs;

public sealed class RequestClassificationJobService(IBackgroundJobClient jobClient)
    : IRequestClassificationJobService
{
    public void EnqueueClassification(
        Guid userId,
        string requestType,
        string userRequest,
        string aiResponse)
    {
        jobClient.Enqueue<AiRequestClassificationJob>(
            j => j.ExecuteAsync(userId, requestType, userRequest, aiResponse));
    }
}
