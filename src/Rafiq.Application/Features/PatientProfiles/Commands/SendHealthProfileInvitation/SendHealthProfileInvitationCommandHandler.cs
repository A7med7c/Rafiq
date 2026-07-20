using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.SendHealthProfileInvitation;

public sealed class SendHealthProfileInvitationCommandHandler(
    ICurrentUserService currentUserService,
    IIdentityService identityService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IProfileNotificationService profileNotificationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SendHealthProfileInvitationCommand, ApiResponse<HealthProfileInvitationDto>>
{
    public async Task<ApiResponse<HealthProfileInvitationDto>> Handle(
        SendHealthProfileInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var profile = await patientProfileRepository.GetByIdAsync(request.UserHealthProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.UserHealthProfileId);

        var ownerAccess = await healthProfileAccessRepository.GetActiveOwnerAsync(
            profile.Id,
            currentUserId,
            cancellationToken);

        if (ownerAccess is null)
            throw new UnauthorizedException("Only an active Owner of this profile can send invitations.");

        var invitedUser = await identityService.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new NotFoundException("ApplicationUser", request.Email);

        if (invitedUser.UserId == currentUserId)
            throw new BadRequestException("You cannot invite yourself.");

        if (request.Role == AccessRole.Owner && profile.UserId is not null)
            throw new BadRequestException("A Self Profile cannot have another Owner.");

        var existingAccess = await healthProfileAccessRepository.GetPendingOrActiveAsync(
            profile.Id,
            invitedUser.UserId,
            cancellationToken);

        if (existingAccess is not null)
        {
            throw existingAccess.Status == AccessStatus.Active
                ? new ConflictException("This user already has active access to this profile. Use the Change Role operation to modify their permissions.")
                : new ConflictException("This user already has a pending invitation for this profile.");
        }

        var invitation = HealthProfileAccess.CreateInvitation(
            profile.Id,
            invitedUser.UserId,
            request.Role,
            currentUserId);

        await healthProfileAccessRepository.AddAsync(invitation, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify the invited user (Invitation Sent).
        await profileNotificationService.NotifyInvitationSentAsync(
            profile.Id, invitedUser.UserId, cancellationToken);

        return ApiResponse<HealthProfileInvitationDto>.SuccessResponse(
            invitation.ToDto(),
            "Invitation sent successfully.");
    }
}
