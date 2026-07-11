using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Services;

public sealed class HealthProfileAuthorizationService(
    ICurrentUserService currentUserService,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IPatientProfileRepository patientProfileRepository)
    : IHealthProfileAuthorizationService
{
    public async Task<HealthProfileAccessContext> GetAccessAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var access = await healthProfileAccessRepository.GetActiveAccessAsync(
            userHealthProfileId,
            currentUserId,
            cancellationToken);

        if (access is not null)
        {
            return new HealthProfileAccessContext(
                access.UserHealthProfileId,
                currentUserId,
                access.Id,
                access.Role,
                access.Status);
        }

        var implicitOwnerAccess = await GetImplicitOwnerAccessAsync(
            userHealthProfileId,
            currentUserId,
            cancellationToken);

        if (implicitOwnerAccess is not null)
        {
            return implicitOwnerAccess;
        }

        throw new UnauthorizedException("You do not have access to this health profile.");
    }

    private async Task<HealthProfileAccessContext?> GetImplicitOwnerAccessAsync(
        Guid userHealthProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var profile = await patientProfileRepository.GetByIdAsync(userHealthProfileId, cancellationToken);

        if (profile?.UserId != currentUserId)
            return null;

        return new HealthProfileAccessContext(
            profile.Id,
            currentUserId,
            Guid.Empty,
            Domain.Enums.AccessRole.Owner,
            Domain.Enums.AccessStatus.Active);
    }

    public Task<HealthProfileAccessContext> EnsureCanReadAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => GetAccessAsync(userHealthProfileId, cancellationToken);

    public async Task<HealthProfileAccessContext> EnsureCanWriteAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
    {
        var context = await GetAccessAsync(userHealthProfileId, cancellationToken);

        if (!context.CanWrite)
            throw new UnauthorizedException("You do not have permission to modify this health profile.");

        return context;
    }

    public async Task<HealthProfileAccessContext> EnsureCanManageAccessAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
    {
        var context = await GetAccessAsync(userHealthProfileId, cancellationToken);

        if (!context.CanManageAccess)
            throw new UnauthorizedException("Only an active Owner can manage access for this health profile.");

        return context;
    }
}
