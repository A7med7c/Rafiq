using Hangfire;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services.BackgroundJobs;

public sealed class DocumentAnalysisJobService(IBackgroundJobClient backgroundJobClient)
    : IDocumentAnalysisJobService
{
    public void EnqueueAnalysis(Guid documentId, Guid userId, Guid profileId, string language = "en")
    {
        backgroundJobClient.Enqueue<DocumentAnalysisJob>(
            j => j.ExecuteAsync(documentId, userId, profileId, language));
    }
}
