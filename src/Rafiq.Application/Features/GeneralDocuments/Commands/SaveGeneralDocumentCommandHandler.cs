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
    IUnitOfWork unitOfWork)
    : IRequestHandler<
        SaveGeneralDocumentCommand,
        ApiResponse<GeneralDocumentResponseDto>>
{
    public async Task<ApiResponse<GeneralDocumentResponseDto>> Handle(SaveGeneralDocumentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
    ?? throw new UnauthorizedException("Authentication is required.");

        var document = new GeneralDocument(
    userId,
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