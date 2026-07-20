using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.AcceptAccessRequest;

public sealed class AcceptAccessRequestCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IProfileNotificationService profileNotificationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AcceptAccessRequestCommand, ApiResponse<HealthProfileInvitationDto>>
{
    public async Task<ApiResponse<HealthProfileInvitationDto>> Handle(
        AcceptAccessRequestCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var accessRequest = await healthProfileAccessRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException("HealthProfileAccess", request.RequestId);

        if (accessRequest.Origin != AccessOrigin.AccessRequest)
            throw new BadRequestException("This operation only applies to Access Requests.");

        var targetProfile = await patientProfileRepository.GetByIdAsync(accessRequest.UserHealthProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", accessRequest.UserHealthProfileId);

        if (targetProfile.UserId != currentUserId)
            throw new UnauthorizedException("Only the profile owner can accept this access request.");

        accessRequest.Accept();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify the requester that their request was approved (Access Request Approved).
        await profileNotificationService.NotifyAccessRequestApprovedAsync(
            accessRequest.GranteeUserId, cancellationToken);

        return ApiResponse<HealthProfileInvitationDto>.SuccessResponse(
            accessRequest.ToDto(),
            "Access request accepted successfully.");
    }
}
