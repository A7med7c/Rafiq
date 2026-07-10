using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.UpdateGeneralDocument;

public sealed class UpdateGeneralDocumentCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IGeneralDocumentRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateGeneralDocumentCommand, ApiResponse<GeneralDocumentResponseDto>>
{
    public async Task<ApiResponse<GeneralDocumentResponseDto>> Handle(
        UpdateGeneralDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var profileId = (await patientProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken))?.Id
            ?? throw new NotFoundException("PatientProfile", currentUserId);

        var document = await repository.GetByIdAsync(request.Id, profileId, cancellationToken)
            ?? throw new NotFoundException(nameof(GeneralDocument), request.Id);

        document.Update(
            request.Title,
            request.Description,
            request.AiSummary,
            request.ImagePath);

        repository.Update(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new GeneralDocumentResponseDto
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            AiSummary = document.AiSummary,
            ImagePath = document.ImagePath,
            CreatedAt = document.CreatedAt
        };

        return ApiResponse<GeneralDocumentResponseDto>.SuccessResponse(
            dto,
            "General document updated successfully.");
    }
}