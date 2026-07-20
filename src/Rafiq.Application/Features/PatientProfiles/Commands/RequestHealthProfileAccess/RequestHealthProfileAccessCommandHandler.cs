using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.RequestHealthProfileAccess;

public sealed class RequestHealthProfileAccessCommandHandler(
    ICurrentUserService currentUserService,
    IIdentityService identityService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IProfileNotificationService profileNotificationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RequestHealthProfileAccessCommand, ApiResponse<HealthProfileInvitationDto>>
{
    public async Task<ApiResponse<HealthProfileInvitationDto>> Handle(
        RequestHealthProfileAccessCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var targetUser = await identityService.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new NotFoundException("ApplicationUser", request.Email);

        if (targetUser.UserId == currentUserId)
            throw new BadRequestException("You cannot request access to your own profile.");

        var targetProfile = await patientProfileRepository.GetByUserIdAsync(targetUser.UserId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", targetUser.UserId);

        var existingAccess = await healthProfileAccessRepository.GetPendingOrActiveAsync(
            targetProfile.Id,
            currentUserId,
            cancellationToken);

        if (existingAccess is not null)
        {
            throw existingAccess.Status == AccessStatus.Active
                ? new ConflictException("You already have active access to this profile.")
                : new ConflictException("You already have a pending request or invitation for this profile.");
        }

        var accessRequest = HealthProfileAccess.CreateAccessRequest(
            targetProfile.Id,
            currentUserId,
            request.Role);

        await healthProfileAccessRepository.AddAsync(accessRequest, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify the profile owner(s) that access was requested (Access Request Sent).
        await profileNotificationService.NotifyAccessRequestSentAsync(
            targetProfile.Id, currentUserId, cancellationToken);

        return ApiResponse<HealthProfileInvitationDto>.SuccessResponse(
            accessRequest.ToDto(),
            "Access request sent successfully.");
    }
}
