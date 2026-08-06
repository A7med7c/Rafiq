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
        var fileName = $"{Guid.NewGuid()}{extension}";
        var imagePath = await fileStorageService.UploadFileAsync(
            new MemoryStream(memory.ToArray()),
            fileName,
            "general-documents",
            cancellationToken);

        // Persist a Pending document record — AI fields will be filled by the background job
        var title = Path.GetFileNameWithoutExtension(request.Image.FileName) is { Length: > 0 } n
            ? n
            : "Medical Document";

        var document = new GeneralDocument(
            userHealthProfileId: request.ProfileId,
            title: title,
            description: request.Description?.Trim() ?? string.Empty,
            imagePath: imagePath,
            analysisStatus: GeneralDocumentStatus.Pending);

        await repository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Enqueue the Hangfire job — returns immediately
        analysisJobService.EnqueueAnalysis(document.Id, userId, request.ProfileId, request.Language);

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
