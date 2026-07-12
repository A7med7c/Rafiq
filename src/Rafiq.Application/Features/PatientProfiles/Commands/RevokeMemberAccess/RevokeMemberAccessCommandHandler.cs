using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.RevokeMemberAccess;

public sealed class RevokeMemberAccessCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IHealthProfileAuthorizationService authorizationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeMemberAccessCommand, ApiResponse<HealthProfileInvitationDto>>
{
    public async Task<ApiResponse<HealthProfileInvitationDto>> Handle(
        RevokeMemberAccessCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.ProfileId);

        var context = await authorizationService.EnsureCanManageAccessAsync(request.ProfileId, cancellationToken);

        var targetAccess = await healthProfileAccessRepository.GetByIdAsync(request.AccessId, cancellationToken);

        if (targetAccess is null || targetAccess.UserHealthProfileId != request.ProfileId)
            throw new NotFoundException("HealthProfileAccess", request.AccessId);

        if (targetAccess.GranteeUserId == context.CurrentUserId)
            throw new BadRequestException("You cannot revoke your own access. Use Leave instead.");

        // A Self Profile has exactly one permanent Owner: the account holder.
        if (profile.UserId is not null && targetAccess.GranteeUserId == profile.UserId)
            throw new BadRequestException("The Self Profile owner cannot be revoked.");

        // Never leave a Managed Profile with zero active Owners.
        if (targetAccess.Role == AccessRole.Owner)
        {
            var activeOwnerCount = await healthProfileAccessRepository.CountActiveOwnersAsync(request.ProfileId, cancellationToken);

            if (activeOwnerCount <= 1)
                throw new BadRequestException("A health profile must always have at least one active Owner.");
        }

        targetAccess.Revoke();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<HealthProfileInvitationDto>.SuccessResponse(
            targetAccess.ToDto(),
            "Access revoked successfully.");
    }
}
