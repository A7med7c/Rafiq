using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.DTOs;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.UpdateGeneralDocument;

public sealed record UpdateGeneralDocumentCommand(
    Guid Id,
    string Title,
    string Description,
    string? AiSummary,
    string? ImagePath,
    string? DocumentType,
    string? DoctorName,
    string? HospitalOrClinic,
    string? DocumentDate,
    string? OcrText)
    : IRequest<ApiResponse<GeneralDocumentResponseDto>>;