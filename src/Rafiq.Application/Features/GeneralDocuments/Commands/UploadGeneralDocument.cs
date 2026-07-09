using MediatR;
using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.UploadGeneralDocument;

public sealed record UploadGeneralDocumentCommand(
    IFormFile Image,
    string? Description)
    : IRequest<ApiResponse<GeneralDocumentPreviewDto>>;