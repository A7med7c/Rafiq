using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.DTOs;

namespace Rafiq.Application.Features.GeneralDocuments.Queries.GetMyGeneralDocuments;

public sealed record GetMyGeneralDocumentsQuery(Guid ProfileId)
    : IRequest<ApiResponse<List<GeneralDocumentResponseDto>>>;