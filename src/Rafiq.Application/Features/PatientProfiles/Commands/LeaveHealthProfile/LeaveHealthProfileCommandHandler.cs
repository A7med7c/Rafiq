using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.LeaveHealthProfile;

public sealed class LeaveHealthProfileCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LeaveHealthProfileCommand, ApiResponse<HealthProfileInvitationDto>>
{
    public async Task<ApiResponse<HealthProfileInvitationDto>> Handle(
        LeaveHealthProfileCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var profile = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.ProfileId);

        var myAccess = await healthProfileAccessRepository.GetActiveAccessAsync(request.ProfileId, currentUserId, cancellationToken)
            ?? throw new UnauthorizedException("You do not have active access to this health profile.");

        // The Self Profile's permanent Owner can never leave their own profile.
        if (profile.UserId == currentUserId)
            throw new BadRequestException("The Self Profile owner cannot leave their own profile.");

        // Never leave a Managed Profile with zero active Owners.
        if (myAccess.Role == AccessRole.Owner)
        {
            var activeOwnerCount = await healthProfileAccessRepository.CountActiveOwnersAsync(request.ProfileId, cancellationToken);

            if (activeOwnerCount <= 1)
                throw new BadRequestException("You are the only active Owner of this profile and cannot leave.");
        }

        myAccess.Leave();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<HealthProfileInvitationDto>.SuccessResponse(
            myAccess.ToDto(),
            "You have left this health profile.");
    }
}
