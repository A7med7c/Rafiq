namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// Scoped context populated by background tasks that have no HttpContext.
/// CurrentUserService checks this before falling back to IHttpContextAccessor.
/// </summary>
public interface IBackgroundUserContext
{
    Guid? UserId { get; set; }
}
