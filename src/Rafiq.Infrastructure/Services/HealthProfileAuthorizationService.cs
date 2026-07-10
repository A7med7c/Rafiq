using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Services;

public sealed class HealthProfileAuthorizationService(
    ICurrentUserService currentUserService,
    IHealthProfileAccessRepository healthProfileAccessRepository)
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
            cancellationToken)
            ?? throw new UnauthorizedException("You do not have access to this health profile.");

        return new HealthProfileAccessContext(
            access.UserHealthProfileId,
            currentUserId,
            access.Id,
            access.Role,
            access.Status);
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
