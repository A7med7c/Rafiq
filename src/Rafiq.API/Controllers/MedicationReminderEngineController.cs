using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.MedicationReminderEngine.Commands.ConfirmMedicationReminder;
using Rafiq.Application.Features.MedicationReminderEngine.Commands.SkipMedicationReminder;
using Rafiq.Application.Features.MedicationReminderEngine.Commands.SnoozeMedicationReminder;
using Rafiq.Application.Features.MedicationReminderEngine.Queries.GetMedicationReminderById;
using Rafiq.Application.Features.MedicationReminderEngine.Queries.GetMedicationReminderHistory;
using Rafiq.Application.Features.MedicationReminderEngine.Queries.GetTodaysMedicationReminders;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/medication-reminders")]
public sealed class MedicationReminderEngineController(IMediator mediator) : ControllerBase
{
    [HttpGet("today")]
    public async Task<IActionResult> GetToday(
        [FromQuery] Guid profileId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetTodaysMedicationRemindersQuery(profileId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid profileId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMedicationReminderHistoryQuery(profileId, date),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMedicationReminderByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ConfirmMedicationReminderCommand(id),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/skip")]
    public async Task<IActionResult> Skip(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SkipMedicationReminderCommand(id),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/snooze")]
    public async Task<IActionResult> Snooze(
        Guid id,
        [FromBody] SnoozeRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SnoozeMedicationReminderCommand(id, body.SnoozeMinutes),
            cancellationToken);

        return Ok(result);
    }
}

public sealed record SnoozeRequest(int SnoozeMinutes);
