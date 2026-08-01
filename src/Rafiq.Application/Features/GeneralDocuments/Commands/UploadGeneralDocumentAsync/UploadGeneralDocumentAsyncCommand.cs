using MediatR;
using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.DTOs;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.UploadGeneralDocumentAsync;

public sealed record UploadGeneralDocumentAsyncCommand(
    IFormFile Image,
    Guid ProfileId,
    string? Description)
    : IRequest<ApiResponse<UploadGeneralDocumentAsyncResponseDto>>;
