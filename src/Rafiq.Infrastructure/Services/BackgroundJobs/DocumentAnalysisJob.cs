using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence;

namespace Rafiq.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Hangfire job that runs AI analysis on a GeneralDocument.
/// Uses an atomic DB claim to prevent duplicate processing.
/// Times out after 3 minutes so documents never stay stuck in "Processing".
/// </summary>
public sealed class DocumentAnalysisJob(
    RafiqDbContext db,
    IBedrockService bedrockService,
    IFileStorageService fileStorageService,
    INotificationService notificationService,
    IUserNotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IBackgroundUserContext backgroundUserContext,
    ILogger<DocumentAnalysisJob> logger)
{
    // No automatic retries — a failed AI call should surface to the user, not re-run silently.
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(Guid documentId, Guid userId, Guid profileId)
    {
        backgroundUserContext.UserId = userId;
        var userIdStr = userId.ToString();

        logger.LogInformation("DocumentAnalysisJob starting for documentId={DocId}", documentId);

        // ── Atomic claim: transition Pending → Processing ─────────────────────
        // If another worker already claimed this (e.g., duplicate enqueue), 0 rows are
        // updated and we exit immediately — no duplicate processing.
        var claimed = await db.GeneralDocuments
            .Where(d => d.Id == documentId && d.AnalysisStatus == GeneralDocumentStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.AnalysisStatus, GeneralDocumentStatus.Processing)
                .SetProperty(d => d.ProcessingStartedAt, DateTime.UtcNow)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow));

        if (claimed == 0)
        {
            logger.LogWarning(
                "DocumentAnalysisJob: documentId={DocId} not in Pending state — skipping (already claimed or completed).",
                documentId);
            return;
        }

        // ── Load the document ─────────────────────────────────────────────────
        var document = await db.GeneralDocuments.FindAsync(documentId);
        if (document is null)
        {
            logger.LogError("DocumentAnalysisJob: documentId={DocId} not found after claim.", documentId);
            return;
        }

        // ── 3-minute timeout wraps all AI work ────────────────────────────────
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        try
        {
            // Read file from storage
            var imageBytes = await fileStorageService.GetFileBytesAsync(
                document.ImagePath, timeoutCts.Token);
            var base64 = Convert.ToBase64String(imageBytes);

            // AI analysis
            var extracted = await bedrockService.AnalyzeAsync<BedrockGeneralDocumentDto>(
                base64,
                GeneralDocumentPrompt.Build(),
                timeoutCts.Token);

            if (extracted is null)
                throw new InvalidOperationException("AI returned an empty response.");

            // Validate document category — same rules as the synchronous endpoint
            var category = (extracted.DocumentCategory ?? string.Empty).Trim().ToLowerInvariant();

            string? failureReason = category switch
            {
                "lab"          => "This looks like a lab report. Please upload it in the Lab Analysis section.",
                "imaging"      => "This looks like a radiology/imaging report. Please upload it in the X-Ray & Imaging section.",
                "prescription" => "This looks like a prescription. Please upload it in the Prescriptions section.",
                "medicine"     => "This looks like a medicine box. Please use the Medicine Box section.",
                "not_medical"  => "The uploaded image does not appear to be a medical document.",
                _              => null
            };

            if (failureReason is not null)
            {
                await FailDocumentAsync(document, failureReason, userIdStr, timeoutCts.Token);
                return;
            }

            // Update the document with all AI-extracted fields
            document.Complete(
                title: extracted.DocumentTitle ?? "Medical Document",
                aiSummary: extracted.AiSummary,
                documentType: extracted.DocumentType,
                doctorName: extracted.DoctorName,
                hospitalOrClinic: extracted.HospitalOrClinic,
                documentDate: extracted.DocumentDate,
                ocrText: extracted.OcrText);

            // Persist a DB notification so it appears in the notification centre
            var notif = new UserNotification(
                userId: userId,
                title: "Document Analysis Complete",
                body: $"Your document \"{document.Title}\" has been analyzed successfully.",
                type: "document_analysis",
                titleAr: "اكتملت تحليل المستند",
                bodyAr: $"تم تحليل مستندك \"{document.Title}\" بنجاح.");
            await notificationRepository.AddAsync(notif, timeoutCts.Token);

            await unitOfWork.SaveChangesAsync(timeoutCts.Token);

            logger.LogInformation("DocumentAnalysisJob completed for documentId={DocId}", documentId);

            // Push real-time SignalR event (non-fatal if user is offline)
            try
            {
                await notificationService.SendDocumentAnalysisCompletedAsync(userIdStr, new()
                {
                    DocumentId = document.Id,
                    Title = document.Title,
                    DocumentType = document.DocumentType,
                    AiSummary = document.AiSummary,
                    ImagePath = document.ImagePath,
                }, timeoutCts.Token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "DocumentAnalysisCompleted SignalR push failed (non-fatal) for docId={DocId}", documentId);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("DocumentAnalysisJob timed out for documentId={DocId}", documentId);
            await FailDocumentAsync(document, "Analysis timed out. Please retry.", userIdStr, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DocumentAnalysisJob failed for documentId={DocId}", documentId);
            await FailDocumentAsync(document, "An unexpected error occurred during analysis. Please retry.", userIdStr, CancellationToken.None);
        }
    }

    private async Task FailDocumentAsync(
        GeneralDocument document,
        string reason,
        string userIdStr,
        CancellationToken ct)
    {
        try
        {
            document.Fail(reason);
            await unitOfWork.SaveChangesAsync(ct);

            try
            {
                await notificationService.SendDocumentAnalysisFailedAsync(userIdStr, new()
                {
                    DocumentId = document.Id,
                    Title = document.Title,
                    FailureReason = reason,
                }, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "DocumentAnalysisFailed SignalR push failed (non-fatal) for docId={DocId}", document.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist Failed state for docId={DocId}", document.Id);
        }
    }
}
