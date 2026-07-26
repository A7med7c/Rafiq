using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<ApiResponse<bool>>;
