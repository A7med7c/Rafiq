using Hangfire.Dashboard;

namespace Rafiq.API.Middleware;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.Identity is { IsAuthenticated: true }
            && httpContext.User.IsInRole("Admin");
    }
}
