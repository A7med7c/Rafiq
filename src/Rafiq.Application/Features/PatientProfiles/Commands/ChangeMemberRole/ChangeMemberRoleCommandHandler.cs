using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.ChangeMemberRole;

public sealed class ChangeMemberRoleCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IHealthProfileAuthorizationService authorizationService,
    IProfileNotificationService profileNotificationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeMemberRoleCommand, ApiResponse<HealthProfileInvitationDto>>
{
    public async Task<ApiResponse<HealthProfileInvitationDto>> Handle(
        ChangeMemberRoleCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.ProfileId);

        var context = await authorizationService.EnsureCanManageAccessAsync(request.ProfileId, cancellationToken);

        var targetAccess = await healthProfileAccessRepository.GetByIdAsync(request.AccessId, cancellationToken);

        if (targetAccess is null || targetAccess.UserHealthProfileId != request.ProfileId)
            throw new NotFoundException("HealthProfileAccess", request.AccessId);

        if (targetAccess.Status != AccessStatus.Active)
            throw new BadRequestException("Only active access can have its role changed.");

        if (targetAccess.GranteeUserId == context.CurrentUserId)
            throw new BadRequestException("You cannot change your own role.");

        // A Self Profile has exactly one permanent Owner: the account holder.
        if (profile.UserId is not null && targetAccess.GranteeUserId == profile.UserId)
            throw new BadRequestException("The Self Profile owner's role cannot be changed.");

        if (profile.UserId is not null && request.Role == AccessRole.Owner)
            throw new BadRequestException("A Self Profile cannot have another Owner.");

        // Never leave a Managed Profile with zero active Owners.
        if (targetAccess.Role == AccessRole.Owner && request.Role != AccessRole.Owner)
        {
            var activeOwnerCount = await healthProfileAccessRepository.CountActiveOwnersAsync(request.ProfileId, cancellationToken);

            if (activeOwnerCount <= 1)
                throw new BadRequestException("A health profile must always have at least one active Owner.");
        }

        targetAccess.ChangeRole(request.Role);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify the affected member that their role changed (Role Changed).
        await profileNotificationService.NotifyRoleChangedAsync(
            targetAccess.GranteeUserId, cancellationToken);

        return ApiResponse<HealthProfileInvitationDto>.SuccessResponse(
            targetAccess.ToDto(),
            "Member role updated successfully.");
    }
}
