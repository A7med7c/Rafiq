using MediatR;
using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.UploadGeneralDocumentAsync;

public sealed class UploadGeneralDocumentAsyncCommandHandler(
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService,
    IGeneralDocumentRepository repository,
    IDocumentAnalysisJobService analysisJobService,
    IDuplicateDocumentDetector duplicateDetector,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadGeneralDocumentAsyncCommand, ApiResponse<UploadGeneralDocumentAsyncResponseDto>>
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };

    public async Task<ApiResponse<UploadGeneralDocumentAsyncResponseDto>> Handle(
        UploadGeneralDocumentAsyncCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        // Fast synchronous validation — no AI involved yet
        var extension = Path.GetExtension(request.Image.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new BadRequestException("Unsupported file type. Please upload a JPG, PNG, WebP, or PDF.");

        if (request.Image.Length > 20 * 1024 * 1024)
            throw new BadRequestException("File is too large. Maximum size is 20 MB.");

        // Upload the file immediately
        using var memory = new MemoryStream();
        await request.Image.CopyToAsync(memory, cancellationToken);
        var imageBytes = memory.ToArray();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var imagePath = await fileStorageService.UploadFileAsync(
            new MemoryStream(imageBytes),
            fileName,
            "general-documents",
            cancellationToken);

        // ── Duplicate check ───────────────────────────────────────────
        var duplicateCheck = await duplicateDetector.ComputeHashAndCheckAsync(
            imageBytes,
            imagePath,
            request.ProfileId,
            userId,
            cancellationToken);

        if (duplicateCheck.IsDuplicate)
        {
            if (duplicateCheck.IsSameProfile)
            {
                throw new DocumentValidationException("DUPLICATE_DOCUMENT", "This exact document has already been uploaded to this profile.");
            }

            if (!request.BypassFamilyDuplicateCheck)
            {
                return ApiResponse<UploadGeneralDocumentAsyncResponseDto>.FailureResponse(
                    "This document already exists in another family member's profile.",
                    errorCode: "DuplicateInFamily",
                    errorData: new
                    {
                        existingProfileId = duplicateCheck.ExistingProfileId,
                        existingProfileName = duplicateCheck.ExistingProfileName
                    });
            }

            // User confirmed — copy AI data from existing document, skip background job
            if (duplicateCheck.ExistingDocumentId.HasValue)
            {
                var source = await repository.GetByIdAsync(duplicateCheck.ExistingDocumentId.Value, cancellationToken);
                if (source is not null && source.AnalysisStatus == GeneralDocumentStatus.Completed)
                {
                    var title = source.Title;
                    var document = new GeneralDocument(
                        userHealthProfileId: request.ProfileId,
                        title: title,
                        description: request.Description?.Trim() ?? string.Empty,
                        imagePath: imagePath,
                        fileHash: duplicateCheck.FileHash);

                    document.Complete(
                        title: source.Title,
                        aiSummary: source.AiSummary,
                        documentType: source.DocumentType,
                        doctorName: source.DoctorName,
                        hospitalOrClinic: source.HospitalOrClinic,
                        documentDate: source.DocumentDate,
                        ocrText: source.OcrText,
                        medicalAttentionReason: source.MedicalAttentionReason,
                        recommendedSpecialty: source.RecommendedSpecialty,
                        confidenceScore: source.ConfidenceScore);

                    await repository.AddAsync(document, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    return ApiResponse<UploadGeneralDocumentAsyncResponseDto>.SuccessResponse(
                        new UploadGeneralDocumentAsyncResponseDto
                        {
                            DocumentId = document.Id,
                            ImagePath = imagePath,
                            Title = title,
                        },
                        "Document uploaded. AI analysis has started.");
                }
            }
        }
        // ─────────────────────────────────────────────────────────────

        // Persist a Pending document record — AI fields will be filled by the background job
        var docTitle = Path.GetFileNameWithoutExtension(request.Image.FileName) is { Length: > 0 } n
            ? n
            : "Medical Document";

        var newDocument = new GeneralDocument(
            userHealthProfileId: request.ProfileId,
            title: docTitle,
            description: request.Description?.Trim() ?? string.Empty,
            imagePath: imagePath,
            analysisStatus: GeneralDocumentStatus.Pending,
            fileHash: duplicateCheck.FileHash);

        await repository.AddAsync(newDocument, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Enqueue the Hangfire job — returns immediately
        analysisJobService.EnqueueAnalysis(newDocument.Id, userId, request.ProfileId, request.Language);

        return ApiResponse<UploadGeneralDocumentAsyncResponseDto>.SuccessResponse(
            new UploadGeneralDocumentAsyncResponseDto
            {
                DocumentId = newDocument.Id,
                ImagePath = imagePath,
                Title = docTitle,
            },
            "Document uploaded. AI analysis has started.");
    }
}
