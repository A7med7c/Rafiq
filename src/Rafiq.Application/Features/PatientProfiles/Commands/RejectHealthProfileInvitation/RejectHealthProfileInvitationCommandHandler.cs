using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.RejectHealthProfileInvitation;

public sealed class RejectHealthProfileInvitationCommandHandler(
    ICurrentUserService currentUserService,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RejectHealthProfileInvitationCommand, ApiResponse<HealthProfileInvitationDto>>
{
    public async Task<ApiResponse<HealthProfileInvitationDto>> Handle(
        RejectHealthProfileInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var invitation = await healthProfileAccessRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException("HealthProfileAccess", request.InvitationId);

        if (invitation.Origin != AccessOrigin.GrantInvitation)
            throw new BadRequestException("This operation only applies to Grant Invitations.");

        if (invitation.GranteeUserId != currentUserId)
            throw new UnauthorizedException("Only the invited user can reject this invitation.");

        invitation.Reject();

        healthProfileAccessRepository.Update(invitation);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<HealthProfileInvitationDto>.SuccessResponse(
            invitation.ToDto(),
            "Invitation rejected successfully.");
    }
}
