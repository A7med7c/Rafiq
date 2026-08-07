using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.Commands.SaveGeneralDocument;
using Rafiq.Application.Features.GeneralDocuments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;


public sealed class SaveGeneralDocumentCommandHandler(
    ICurrentUserService currentUserService,
    IGeneralDocumentRepository repository,
    IUnitOfWork unitOfWork,
    IHealthSummaryCacheRepository summaryCache,
    IMedicalWarningCalculator warningCalculator)
    : IRequestHandler<
        SaveGeneralDocumentCommand,
        ApiResponse<GeneralDocumentResponseDto>>
{
    public async Task<ApiResponse<GeneralDocumentResponseDto>> Handle(SaveGeneralDocumentCommand request, CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        if (await repository.ExistsDuplicateAsync(request.ProfileId, request.Title, cancellationToken))
            throw new ConflictException("This document already exists in your medical records.");

        var document = new GeneralDocument(
            request.ProfileId,
            request.Title,
            request.Description,
            request.ImagePath,
            request.AiSummary,
            request.DocumentType,
            request.DoctorName,
            request.HospitalOrClinic,
            request.DocumentDate,
            request.OcrText,
            request.MedicalAttentionReason,
            request.RecommendedSpecialty,
            request.ConfidenceScore);

        await repository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(request.ProfileId, cancellationToken);

        return ApiResponse<GeneralDocumentResponseDto>.SuccessResponse(
            new GeneralDocumentResponseDto
            {
                Id = document.Id,
                Title = document.Title,
                Description = document.Description,
                AiSummary = document.AiSummary,
                ImagePath = document.ImagePath,
                DocumentType = document.DocumentType,
                DoctorName = document.DoctorName,
                HospitalOrClinic = document.HospitalOrClinic,
                DocumentDate = document.DocumentDate,
                OcrText = document.OcrText,
                MedicalAttentionReason = document.MedicalAttentionReason,
                RecommendedSpecialty = document.RecommendedSpecialty,
                ConfidenceScore = document.ConfidenceScore,
                RequiresMedicalAttention = warningCalculator.RequiresMedicalAttention(document.ConfidenceScore),
                AttentionLevel = warningCalculator.ComputeAttentionLevel(document.ConfidenceScore).ToString(),
                CreatedAt = document.CreatedAt
            },
            "General document saved successfully.");
    }
}