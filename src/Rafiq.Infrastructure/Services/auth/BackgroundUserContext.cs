using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services.auth;

public sealed class BackgroundUserContext : IBackgroundUserContext
{
    public Guid? UserId { get; set; }
}
