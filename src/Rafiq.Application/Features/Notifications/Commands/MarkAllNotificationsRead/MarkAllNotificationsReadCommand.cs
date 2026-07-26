using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

public record MarkAllNotificationsReadCommand : IRequest<ApiResponse<bool>>;
