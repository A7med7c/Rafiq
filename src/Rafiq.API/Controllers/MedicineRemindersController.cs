using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.MedicineReminders.Commands.CreateMedicineReminders;
using Rafiq.Application.Features.MedicineReminders.Commands.DeleteMedicineReminder;
using Rafiq.Application.Features.MedicineReminders.Commands.ToggleMedicineReminderStatus;
using Rafiq.Application.Features.MedicineReminders.Commands.UpdateMedicineReminder;
using Rafiq.Application.Features.MedicineReminders.Queries.GetMedicineReminderById;
using Rafiq.Application.Features.MedicineReminders.Queries.GetMedicineReminders;
using Rafiq.Domain.Exceptions;

namespace Rafiq.API.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class MedicineRemindersController(ISender sender) : ControllerBase
{
    [HttpPost("user-medicines/{medicineId:guid}/reminders")]
    public async Task<IActionResult> Create(Guid medicineId, CreateMedicineRemindersCommand command)
    {
        if (medicineId != command.UserMedicineId)
        {
            throw new BadRequestException("MedicineId in URL must match UserMedicineId in body.");
        }
        var result = await sender.Send(command);
        return Ok(result);
    }

    [HttpGet("user-medicines/{medicineId:guid}/reminders")]
    public async Task<IActionResult> GetAllForMedicine(Guid medicineId)
    {
        var result = await sender.Send(new GetMedicineRemindersQuery(medicineId));
        return Ok(result);
    }

    [HttpGet("medicine-reminders/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetMedicineReminderByIdQuery(id));
        return Ok(result);
    }

    [HttpPut("medicine-reminders/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMedicineReminderCommand command)
    {
        if (id != command.Id)
        {
            throw new BadRequestException("Id in URL must match Id in body.");
        }
        var result = await sender.Send(command);
        return Ok(result);
    }

    [HttpDelete("medicine-reminders/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteMedicineReminderCommand(id));
        return Ok(result);
    }

    [HttpPatch("medicine-reminders/{id:guid}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await sender.Send(new ToggleMedicineReminderStatusCommand(id));
        return Ok(result);
    }
}
