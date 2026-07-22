using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.GeneralDocuments.Queries.GetMyGeneralDocuments;

public sealed class GetMyGeneralDocumentsQueryHandler(
    ICurrentUserService currentUserService,
    IGeneralDocumentRepository repository)
    : IRequestHandler<GetMyGeneralDocumentsQuery, ApiResponse<List<GeneralDocumentResponseDto>>>
{
    public async Task<ApiResponse<List<GeneralDocumentResponseDto>>> Handle(
        GetMyGeneralDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var documents = await repository.GetAllByUserIdAsync(request.ProfileId, cancellationToken);

        var dtos = documents.Select(document => new GeneralDocumentResponseDto
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
            CreatedAt = document.CreatedAt
        }).ToList();

        return ApiResponse<List<GeneralDocumentResponseDto>>.SuccessResponse(
            dtos,
            "General documents retrieved successfully.");
    }
}