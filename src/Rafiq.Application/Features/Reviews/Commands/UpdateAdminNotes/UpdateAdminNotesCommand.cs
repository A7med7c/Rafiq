using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Reviews.Commands.UpdateAdminNotes;

public sealed record UpdateAdminNotesCommand(Guid ReviewId, string? Notes)
    : IRequest<ApiResponse<bool>>;
