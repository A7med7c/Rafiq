using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.GeneralDocuments.Queries.GetGeneralDocumentStatus;

public sealed class GetGeneralDocumentStatusQueryHandler(
    IGeneralDocumentRepository repository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetGeneralDocumentStatusQuery, ApiResponse<GeneralDocumentResponseDto>>
{
    public async Task<ApiResponse<GeneralDocumentResponseDto>> Handle(
        GetGeneralDocumentStatusQuery request,
        CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var document = await repository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new NotFoundException("GeneralDocument", request.DocumentId);

        var dto = new GeneralDocumentResponseDto
        {
            Id             = document.Id,
            Title          = document.Title,
            Description    = document.Description,
            AiSummary      = document.AiSummary,
            ImagePath      = document.ImagePath,
            DocumentType   = document.DocumentType,
            DoctorName     = document.DoctorName,
            HospitalOrClinic = document.HospitalOrClinic,
            DocumentDate   = document.DocumentDate,
            OcrText        = document.OcrText,
            CreatedAt      = document.CreatedAt,
            AnalysisStatus = document.AnalysisStatus.ToString(),
            FailureReason  = document.FailureReason,
        };

        return ApiResponse<GeneralDocumentResponseDto>.SuccessResponse(dto);
    }
}
