using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.EmergencyContacts.Commands.CreateEmergencyContact;
using Rafiq.Application.Features.EmergencyContacts.Commands.DeleteEmergencyContact;
using Rafiq.Application.Features.EmergencyContacts.Queries.GetEmergencyContacts;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/emergency-contacts")]
public sealed class EmergencyContactsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateEmergencyContact(
        [FromBody] CreateEmergencyContactCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetEmergencyContacts(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEmergencyContactsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEmergencyContact(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteEmergencyContactCommand(id), cancellationToken);
        return Ok(result);
    }
}
