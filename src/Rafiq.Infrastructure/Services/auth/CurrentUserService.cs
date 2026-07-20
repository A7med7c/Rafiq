using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Interfaces;
using System.Security.Claims;

namespace Rafiq.Infrastructure.Services.auth;

public sealed class CurrentUserService(
    IHttpContextAccessor _httpContextAccessor,
    IBackgroundUserContext _backgroundUserContext) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            // Background tasks have no HttpContext — check the scoped context first.
            if (_backgroundUserContext.UserId.HasValue)
                return _backgroundUserContext.UserId;

            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    public bool IsInRole(string role) => _httpContextAccessor.HttpContext?.User.IsInRole(role) == true;
}
