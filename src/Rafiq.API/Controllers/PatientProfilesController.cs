using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.PatientProfiles.Commands.AcceptAccessRequest;
using Rafiq.Application.Features.PatientProfiles.Commands.AcceptHealthProfileInvitation;
using Rafiq.Application.Features.PatientProfiles.Commands.CancelHealthProfileInvitation;
using Rafiq.Application.Features.PatientProfiles.Commands.ChangeMemberRole;
using Rafiq.Application.Features.PatientProfiles.Commands.CreateManagedProfile;
using Rafiq.Application.Features.PatientProfiles.Commands.CreatePatientProfile;
using Rafiq.Application.Features.PatientProfiles.Commands.DeletePatientProfile;
using Rafiq.Application.Features.PatientProfiles.Commands.LeaveHealthProfile;
using Rafiq.Application.Features.PatientProfiles.Commands.RejectAccessRequest;
using Rafiq.Application.Features.PatientProfiles.Commands.RejectHealthProfileInvitation;
using Rafiq.Application.Features.PatientProfiles.Commands.RequestHealthProfileAccess;
using Rafiq.Application.Features.PatientProfiles.Commands.RevokeMemberAccess;
using Rafiq.Application.Features.PatientProfiles.Commands.SendHealthProfileInvitation;
using Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;
using Rafiq.Application.Features.PatientProfiles.Commands.UpdateProfileImage;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Application.Features.PatientProfiles.Queries.GetAccessibleHealthProfiles;
using Rafiq.Application.Features.PatientProfiles.Queries.GetMyPatientProfile;
using Rafiq.Application.Features.PatientProfiles.Queries.GetPatientProfileById;
using Rafiq.Application.Features.PatientProfiles.Queries.GetProfileMembers;
using Rafiq.Application.Features.PatientProfiles.Queries.GetReceivedAccessRequests;
using Rafiq.Application.Features.PatientProfiles.Queries.GetReceivedHealthProfileInvitations;
using Rafiq.Application.Features.PatientProfiles.Queries.GetSentHealthProfileInvitations;
using Rafiq.Domain.Exceptions;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/patient-profiles")]
public sealed class PatientProfilesController(IMediator _mediator) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("managed")]
    public async Task<IActionResult> CreateManaged([FromBody] CreateManagedProfileCommand command, CancellationToken cancellationToken)
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

    [HttpGet("accessible")]
    public async Task<IActionResult> GetAccessible(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAccessibleHealthProfilesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPatientProfileByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientProfileCommand command, CancellationToken cancellationToken)
    {
        if (id != command.PatientProfileId)
            throw new BadRequestException("Route id doesn't match body id.");

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeletePatientProfileCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UpdateProfileImage(
         Guid id,
         [FromForm] UpdateProfileImageRequestDto request,
         CancellationToken cancellationToken)
    {
        var command = new UpdateProfileImageCommand(
            id,
            request.ProfileImage,
            request.RemoveImage);

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{profileId:guid}/invitations")]
    public async Task<IActionResult> SendInvitation(
            Guid profileId,
            [FromBody] SendHealthProfileInvitationRequestDto request,
            CancellationToken cancellationToken)
    {
        var command = new SendHealthProfileInvitationCommand(profileId, request.Email, request.Role);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("invitations/received")]
    public async Task<IActionResult> GetReceivedInvitations(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReceivedHealthProfileInvitationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("invitations/sent")]
    public async Task<IActionResult> GetSentInvitations(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSentHealthProfileInvitationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("invitations/{invitationId:guid}/accept")]
    public async Task<IActionResult> AcceptInvitation(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AcceptHealthProfileInvitationCommand(invitationId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("invitations/{invitationId:guid}/reject")]
    public async Task<IActionResult> RejectInvitation(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RejectHealthProfileInvitationCommand(invitationId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("invitations/{invitationId:guid}/cancel")]
    public async Task<IActionResult> CancelInvitation(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelHealthProfileInvitationCommand(invitationId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("access-requests")]
    public async Task<IActionResult> RequestAccess(
        [FromBody] RequestHealthProfileAccessCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("access-requests/received")]
    public async Task<IActionResult> GetReceivedAccessRequests(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReceivedAccessRequestsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("access-requests/{requestId:guid}/accept")]
    public async Task<IActionResult> AcceptAccessRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AcceptAccessRequestCommand(requestId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("access-requests/{requestId:guid}/reject")]
    public async Task<IActionResult> RejectAccessRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RejectAccessRequestCommand(requestId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{profileId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid profileId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProfileMembersQuery(profileId), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{profileId:guid}/members/{accessId:guid}/role")]
    public async Task<IActionResult> ChangeMemberRole(
        Guid profileId,
        Guid accessId,
        [FromBody] ChangeMemberRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeMemberRoleCommand(profileId, accessId, request.Role);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{profileId:guid}/members/{accessId:guid}/revoke")]
    public async Task<IActionResult> RevokeMemberAccess(Guid profileId, Guid accessId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RevokeMemberAccessCommand(profileId, accessId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{profileId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid profileId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LeaveHealthProfileCommand(profileId), cancellationToken);
        return Ok(result);
    }
}

