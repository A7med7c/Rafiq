using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetProfileMembers;

public sealed class GetProfileMembersQueryHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IHealthProfileAuthorizationService authorizationService,
    IIdentityService identityService)
    : IRequestHandler<GetProfileMembersQuery, ApiResponse<IReadOnlyList<ProfileMemberDto>>>
{
    public async Task<ApiResponse<IReadOnlyList<ProfileMemberDto>>> Handle(
        GetProfileMembersQuery request,
        CancellationToken cancellationToken)
    {
        _ = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.ProfileId);

        var context = await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var members = await healthProfileAccessRepository.GetActiveMembersAsync(request.ProfileId, cancellationToken);

        var grantees = new Dictionary<Guid, AccountDto>();

        foreach (var granteeId in members.Select(x => x.GranteeUserId).Distinct())
        {
            grantees[granteeId] = await identityService.GetAccountAsync(granteeId, cancellationToken);
        }

        var dtos = members
            .OrderBy(x => RoleSortOrder(x.Role))
            .ThenBy(x => x.CreatedAt)
            .Select(x => x.ToProfileMemberDto(grantees[x.GranteeUserId], context.CurrentUserId))
            .ToList();

        return ApiResponse<IReadOnlyList<ProfileMemberDto>>.SuccessResponse(dtos);
    }

    private static int RoleSortOrder(AccessRole role) => role switch
    {
        AccessRole.Owner => 0,
        AccessRole.Manager => 1,
        _ => 2
    };
}
