using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.DTOs;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.SaveGeneralDocument;

public sealed record SaveGeneralDocumentCommand(
    string Title,
    string Description,
    string? AiSummary,
    string ImagePath)
    : IRequest<ApiResponse<GeneralDocumentResponseDto>>;