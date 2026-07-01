namespace Rafiq.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
