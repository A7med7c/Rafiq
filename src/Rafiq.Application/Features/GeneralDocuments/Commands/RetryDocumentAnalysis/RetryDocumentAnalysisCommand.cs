using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.RetryDocumentAnalysis;

public sealed record RetryDocumentAnalysisCommand(Guid DocumentId, string Language = "en")
    : IRequest<ApiResponse<bool>>;
