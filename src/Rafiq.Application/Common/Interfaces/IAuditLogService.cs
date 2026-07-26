using Rafiq.Application.Features.Admin;

namespace Rafiq.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        Guid actorId,
        string actorName,
        string actorEmail,
        string module,
        string action,
        string target,
        string severity,
        string description,
        IReadOnlyList<(string Field, string? Before, string? After)> changes,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogDto>> GetPagedAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}
