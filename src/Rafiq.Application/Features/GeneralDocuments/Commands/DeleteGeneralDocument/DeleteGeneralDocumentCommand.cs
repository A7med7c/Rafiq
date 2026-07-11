using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.DeleteGeneralDocument;

public sealed record DeleteGeneralDocumentCommand(Guid Id) : IRequest<ApiResponse<bool>>;