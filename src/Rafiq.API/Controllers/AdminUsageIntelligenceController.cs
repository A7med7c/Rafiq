using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Admin;
using Rafiq.Domain.Constants;
using Rafiq.Domain.Exceptions;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin/usage-intelligence")]
public sealed class AdminUsageIntelligenceController(
    IUsageIntelligenceService service,
    ICurrentUserService currentUserService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var result = await service.GetOverviewAsync(ct);
        return Ok(ApiResponse<UsageIntelligenceOverviewDto>.SuccessResponse(result));
    }

    [HttpGet("attention-queue")]
    public async Task<IActionResult> GetAttentionQueue(
        [FromQuery] UsageAttentionQueueQuery query,
        CancellationToken ct)
    {
        var result = await service.GetAttentionQueueAsync(query, ct);
        return Ok(ApiResponse<PagedResult<UsageAttentionUserDto>>.SuccessResponse(result));
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUserDetail(Guid userId, CancellationToken ct)
    {
        var result = await service.GetUserDetailAsync(userId, ct);
        return Ok(ApiResponse<UsageUserDetailDto>.SuccessResponse(result));
    }

    [HttpPost("users/{userId:guid}/actions")]
    public async Task<IActionResult> TakeAction(
        Guid userId,
        [FromBody] TakeUsageActionDto dto,
        CancellationToken ct)
    {
        var adminId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var admin     = await userManager.FindByIdAsync(adminId.ToString());
        var adminName = admin is not null
            ? $"{admin.FirstName} {admin.LastName}".Trim()
            : "Admin";

        await service.TakeActionAsync(userId, adminId, adminName, dto, ct);
        return Ok(ApiResponseBase.SuccessResponse($"Action '{dto.ActionType}' recorded successfully."));
    }
}
