using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Admin;
using Rafiq.Domain.Constants;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin")]
public sealed class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await adminService.GetDashboardAsync(cancellationToken);
        return Ok(ApiResponse<AdminDashboardDto>.SuccessResponse(result));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] AdminUserQuery query,
        CancellationToken cancellationToken)
    {
        var result = await adminService.GetUsersAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<AdminUserListItemDto>>.SuccessResponse(result));
    }

    [HttpPatch("users/{userId:guid}/status")]
    public async Task<IActionResult> SetUserStatus(
        Guid userId,
        [FromBody] SetAdminUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await adminService.SetUserActiveStatusAsync(
            actorUserId,
            userId,
            request.IsActive,
            cancellationToken);

        return Ok(ApiResponseBase.SuccessResponse(
            request.IsActive ? "User activated successfully." : "User deactivated successfully."));
    }
}

public sealed record SetAdminUserStatusRequest(bool IsActive);
