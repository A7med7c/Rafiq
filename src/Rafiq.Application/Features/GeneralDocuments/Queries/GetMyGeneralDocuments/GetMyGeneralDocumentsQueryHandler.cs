using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.GeneralDocuments.Queries.GetMyGeneralDocuments;

public sealed class GetMyGeneralDocumentsQueryHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IGeneralDocumentRepository repository)
    : IRequestHandler<GetMyGeneralDocumentsQuery, ApiResponse<List<GeneralDocumentResponseDto>>>
{
    public async Task<ApiResponse<List<GeneralDocumentResponseDto>>> Handle(
        GetMyGeneralDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var profileId = (await patientProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken))?.Id
            ?? throw new NotFoundException("PatientProfile", currentUserId);

        var documents = await repository.GetAllByUserIdAsync(profileId, cancellationToken);

        var dtos = documents.Select(document => new GeneralDocumentResponseDto
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            AiSummary = document.AiSummary,
            ImagePath = document.ImagePath,
            CreatedAt = document.CreatedAt
        }).ToList();

        return ApiResponse<List<GeneralDocumentResponseDto>>.SuccessResponse(
            dtos,
            "General documents retrieved successfully.");
    }
}