using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.PatientProfiles.Commands.CreatePatientProfile;
using Rafiq.Application.Features.PatientProfiles.Commands.DeletePatientProfile;
using Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;
using Rafiq.Application.Features.PatientProfiles.Queries.GetMyPatientProfile;
using Rafiq.Application.Features.PatientProfiles.Queries.GetPatientProfileById;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/patient-profiles")]
public sealed class PatientProfilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PatientProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyPatientProfileQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPatientProfileByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePatientProfileCommand(
            id,
            request.FullName,
            request.DateOfBirth,
            request.Gender,
            request.BloodType,
            request.Allergies,
            request.ChronicConditions,
            request.EmergencyContactName,
            request.EmergencyContactPhone);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeletePatientProfileCommand(id), cancellationToken);
        return Ok(result);
    }
}

public sealed record UpdatePatientProfileRequest(
    string FullName,
    DateTime DateOfBirth,
    string Gender,
    string? BloodType,
    string? Allergies,
    string? ChronicConditions,
    string EmergencyContactName,
    string EmergencyContactPhone);
