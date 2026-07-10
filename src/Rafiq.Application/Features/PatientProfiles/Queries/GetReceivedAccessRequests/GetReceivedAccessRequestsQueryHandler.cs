using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetReceivedAccessRequests;

public sealed class GetReceivedAccessRequestsQueryHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IIdentityService identityService)
    : IRequestHandler<GetReceivedAccessRequestsQuery, ApiResponse<IReadOnlyList<ReceivedAccessRequestDto>>>
{
    public async Task<ApiResponse<IReadOnlyList<ReceivedAccessRequestDto>>> Handle(
        GetReceivedAccessRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var myProfile = await patientProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken);

        if (myProfile is null)
        {
            return ApiResponse<IReadOnlyList<ReceivedAccessRequestDto>>.SuccessResponse(
                Array.Empty<ReceivedAccessRequestDto>());
        }

        var accessRequests = await healthProfileAccessRepository.GetPendingReceivedAccessRequestsAsync(
            myProfile.Id,
            cancellationToken);

        var requesters = new Dictionary<Guid, AccountDto>();

        foreach (var requesterId in accessRequests.Select(x => x.GranteeUserId).Distinct())
        {
            requesters[requesterId] = await identityService.GetAccountAsync(requesterId, cancellationToken);
        }

        var dtos = accessRequests
            .Select(x => x.ToReceivedAccessRequestDto(requesters[x.GranteeUserId]))
            .ToList();

        return ApiResponse<IReadOnlyList<ReceivedAccessRequestDto>>.SuccessResponse(dtos);
    }
}
