using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetSentHealthProfileInvitations;

public sealed class GetSentHealthProfileInvitationsQueryHandler(
    ICurrentUserService currentUserService,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IIdentityService identityService)
    : IRequestHandler<GetSentHealthProfileInvitationsQuery, ApiResponse<IReadOnlyList<SentHealthProfileInvitationDto>>>
{
    public async Task<ApiResponse<IReadOnlyList<SentHealthProfileInvitationDto>>> Handle(
        GetSentHealthProfileInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var invitations = await healthProfileAccessRepository.GetSentInvitationsAsync(
            currentUserId,
            cancellationToken);

        var grantees = new Dictionary<Guid, AccountDto>();

        foreach (var granteeId in invitations.Select(x => x.GranteeUserId).Distinct())
        {
            grantees[granteeId] = await identityService.GetAccountAsync(granteeId, cancellationToken);
        }

        var dtos = invitations
            .Select(x => x.ToSentInvitationDto(grantees[x.GranteeUserId]))
            .ToList();

        return ApiResponse<IReadOnlyList<SentHealthProfileInvitationDto>>.SuccessResponse(dtos);
    }
}
