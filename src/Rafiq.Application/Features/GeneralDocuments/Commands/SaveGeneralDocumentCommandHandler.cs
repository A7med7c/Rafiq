using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.Commands.SaveGeneralDocument;
using Rafiq.Application.Features.GeneralDocuments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

public sealed class SaveGeneralDocumentCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IGeneralDocumentRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<
        SaveGeneralDocumentCommand,
        ApiResponse<GeneralDocumentResponseDto>>
{
    public async Task<ApiResponse<GeneralDocumentResponseDto>> Handle(SaveGeneralDocumentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
    ?? throw new UnauthorizedException("Authentication is required.");

        var profile = await patientProfileRepository.GetByUserIdAsync(userId, cancellationToken)
    ?? throw new NotFoundException("UserHealthProfile", userId);

        var document = new GeneralDocument(
    profile.Id,
    request.Title,
    request.Description,
    request.ImagePath,
    request.AiSummary);

        await repository.AddAsync(
    document,
    cancellationToken);


        return ApiResponse<GeneralDocumentResponseDto>.SuccessResponse(

    new GeneralDocumentResponseDto
    {
        Id = document.Id,

        Title = document.Title,

        Description = document.Description,

        AiSummary = document.AiSummary,

        ImagePath = document.ImagePath,

        CreatedAt = document.CreatedAt
    },

    "General document saved successfully.");
    }
}