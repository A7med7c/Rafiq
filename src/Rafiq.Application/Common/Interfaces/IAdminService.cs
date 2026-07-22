using Rafiq.Application.Features.Admin;

namespace Rafiq.Application.Common.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken = default);

    Task SetUserActiveStatusAsync(
        Guid actorUserId,
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
