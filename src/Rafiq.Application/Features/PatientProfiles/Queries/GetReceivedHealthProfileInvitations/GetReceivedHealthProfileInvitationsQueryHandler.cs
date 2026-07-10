using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetReceivedHealthProfileInvitations;

public sealed class GetReceivedHealthProfileInvitationsQueryHandler(
    ICurrentUserService currentUserService,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IIdentityService identityService)
    : IRequestHandler<GetReceivedHealthProfileInvitationsQuery, ApiResponse<IReadOnlyList<ReceivedHealthProfileInvitationDto>>>
{
    public async Task<ApiResponse<IReadOnlyList<ReceivedHealthProfileInvitationDto>>> Handle(
        GetReceivedHealthProfileInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var invitations = await healthProfileAccessRepository.GetPendingReceivedInvitationsAsync(
            currentUserId,
            cancellationToken);

        var inviters = new Dictionary<Guid, AccountDto>();

        foreach (var inviterId in invitations
            .Where(x => x.InvitedByUserId.HasValue)
            .Select(x => x.InvitedByUserId!.Value)
            .Distinct())
        {
            inviters[inviterId] = await identityService.GetAccountAsync(inviterId, cancellationToken);
        }

        var dtos = invitations
            .Select(x => x.ToReceivedInvitationDto(
                x.InvitedByUserId.HasValue ? inviters.GetValueOrDefault(x.InvitedByUserId.Value) : null))
            .ToList();

        return ApiResponse<IReadOnlyList<ReceivedHealthProfileInvitationDto>>.SuccessResponse(dtos);
    }
}
