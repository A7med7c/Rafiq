using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.Appointments.Commands.CreateAppointment;
using Rafiq.Application.Features.Appointments.Commands.DeleteAppointment;
using Rafiq.Application.Features.Appointments.Commands.UpdateAppointment;
using Rafiq.Application.Features.Appointments.Queries.GetAppointmentById;
using Rafiq.Application.Features.Appointments.Queries.GetAppointments;
using Rafiq.Application.Features.Appointments.Queries.GetTodayAppointments;
using Rafiq.Application.Features.Appointments.Queries.GetUpcomingAppointments;
using Rafiq.Domain.Enums;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/appointments")]
public sealed class AppointmentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAppointmentsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUpcomingAppointmentsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTodayAppointmentsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAppointmentByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAppointmentRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateAppointmentCommand(
                id,
                body.AppointmentType,
                body.CustomType,
                body.Title,
                body.Provider,
                body.AppointmentDateTime,
                body.ReminderOffsetMinutes,
                body.Notes,
                body.Status),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAppointmentCommand(id), cancellationToken);
        return Ok(result);
    }
}

public sealed record UpdateAppointmentRequest(
    AppointmentType AppointmentType,
    string? CustomType,
    string Title,
    string Provider,
    DateTime AppointmentDateTime,
    int? ReminderOffsetMinutes,
    string? Notes,
    AppointmentStatus Status);
