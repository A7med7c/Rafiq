using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using Rafiq.Application.Features.Notifications.Commands.MarkNotificationRead;
using Rafiq.Application.Features.Notifications.Queries.GetMyNotifications;

namespace Rafiq.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMyNotificationsQuery(page, pageSize), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand(id), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
