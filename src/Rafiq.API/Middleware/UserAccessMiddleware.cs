using Microsoft.EntityFrameworkCore;
using Rafiq.Application.Common.Models;
using Rafiq.Infrastructure.Persistence;
using System.Security.Claims;
using System.Text.Json;

namespace Rafiq.API.Middleware;

/// <summary>
/// Checks per-request whether the authenticated user's account is suspended or restricted.
/// This runs after UseAuthentication so the JWT is already validated and the user's claims
/// are available. A single indexed primary-key lookup is performed per authenticated request.
/// </summary>
public sealed class UserAccessMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context, RafiqDbContext db)
    {
        // Only check authenticated requests
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? context.User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            await next(context);
            return;
        }

        var status = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsSuspended, u.IsRestricted })
            .FirstOrDefaultAsync(context.RequestAborted);

        if (status is null)
        {
            await next(context);
            return;
        }

        if (status.IsSuspended)
        {
            await WriteForbidden(context, "Your account has been suspended by an administrator. Please contact support for further information.");
            return;
        }

        if (status.IsRestricted)
        {
            await WriteForbidden(context, "Your account has been restricted by an administrator. Please contact support for assistance.");
            return;
        }

        await next(context);
    }

    private static Task WriteForbidden(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.FailureResponse(message, [message]);
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, _json));
    }
}
