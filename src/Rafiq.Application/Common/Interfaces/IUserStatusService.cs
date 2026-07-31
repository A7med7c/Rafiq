namespace Rafiq.Application.Common.Interfaces;

public sealed record UserStatus(bool IsAiRestricted, bool IsRestricted, bool IsSuspended);

public interface IUserStatusService
{
    Task<UserStatus?> GetStatusAsync(Guid userId, CancellationToken ct = default);
}
