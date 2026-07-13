using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CancelHealthProfileInvitation;

public sealed class CancelHealthProfileInvitationCommandHandler(
    ICurrentUserService currentUserService,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelHealthProfileInvitationCommand, ApiResponse<HealthProfileInvitationDto>>
{
    public async Task<ApiResponse<HealthProfileInvitationDto>> Handle(
        CancelHealthProfileInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var invitation = await healthProfileAccessRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException("HealthProfileAccess", request.InvitationId);

        if (invitation.Origin != AccessOrigin.GrantInvitation)
            throw new BadRequestException("This operation only applies to Grant Invitations.");

        var ownerAccess = await healthProfileAccessRepository.GetActiveOwnerAsync(
            invitation.UserHealthProfileId,
            currentUserId,
            cancellationToken);

        if (ownerAccess is null)
            throw new UnauthorizedException("Only an active Owner of this profile can cancel invitations.");

        invitation.Cancel();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<HealthProfileInvitationDto>.SuccessResponse(
            invitation.ToDto(),
            "Invitation cancelled successfully.");
    }
}
