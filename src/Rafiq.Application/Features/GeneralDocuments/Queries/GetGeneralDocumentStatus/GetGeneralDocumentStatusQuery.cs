using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.DTOs;

namespace Rafiq.Application.Features.GeneralDocuments.Queries.GetGeneralDocumentStatus;

public sealed record GetGeneralDocumentStatusQuery(Guid DocumentId)
    : IRequest<ApiResponse<GeneralDocumentResponseDto>>;
