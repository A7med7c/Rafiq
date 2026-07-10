using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.AcceptHealthProfileInvitation;

public sealed class AcceptHealthProfileInvitationCommandHandler(
    ICurrentUserService currentUserService,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AcceptHealthProfileInvitationCommand, ApiResponse<HealthProfileInvitationDto>>
{
    public async Task<ApiResponse<HealthProfileInvitationDto>> Handle(
        AcceptHealthProfileInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var invitation = await healthProfileAccessRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException("HealthProfileAccess", request.InvitationId);

        if (invitation.Origin != AccessOrigin.GrantInvitation)
            throw new BadRequestException("This operation only applies to Grant Invitations.");

        if (invitation.GranteeUserId != currentUserId)
            throw new UnauthorizedException("Only the invited user can accept this invitation.");

        invitation.Accept();

        healthProfileAccessRepository.Update(invitation);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<HealthProfileInvitationDto>.SuccessResponse(
            invitation.ToDto(),
            "Invitation accepted successfully.");
    }
}
