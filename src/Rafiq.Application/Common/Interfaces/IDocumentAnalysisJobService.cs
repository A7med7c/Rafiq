namespace Rafiq.Application.Common.Interfaces;

public interface IDocumentAnalysisJobService
{
    /// <summary>Enqueues a Hangfire job to run AI analysis on the given document.</summary>
    void EnqueueAnalysis(Guid documentId, Guid userId, Guid profileId, string language = "en");
}
