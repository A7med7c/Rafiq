using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetAccessibleHealthProfiles;

public sealed class GetAccessibleHealthProfilesQueryHandler(
    ICurrentUserService currentUserService,
    IHealthProfileAccessRepository healthProfileAccessRepository)
    : IRequestHandler<GetAccessibleHealthProfilesQuery, ApiResponse<IReadOnlyList<AccessibleHealthProfileDto>>>
{
    public async Task<ApiResponse<IReadOnlyList<AccessibleHealthProfileDto>>> Handle(
        GetAccessibleHealthProfilesQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var accessRecords = await healthProfileAccessRepository.GetActiveAccessibleProfilesAsync(
            currentUserId,
            cancellationToken);

        var profiles = accessRecords
            .Select(x => x.ToAccessibleProfileDto(currentUserId))
            .OrderBy(x => ProfileTypeSortOrder(x.ProfileType))
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToList();

        return ApiResponse<IReadOnlyList<AccessibleHealthProfileDto>>.SuccessResponse(profiles);
    }

    private static int ProfileTypeSortOrder(string profileType) => profileType switch
    {
        "Self" => 0,
        "Managed" => 1,
        _ => 2
    };
}
