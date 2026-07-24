using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Admin;
using Rafiq.Domain.Constants;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin/ai")]
public sealed class AdminAiController(IAdminAiService adminAiService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await adminAiService.GetOverviewAsync(cancellationToken);
        return Ok(ApiResponse<AiOverviewDto>.SuccessResponse(result));
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] AiRequestQuery query,
        CancellationToken cancellationToken)
    {
        var result = await adminAiService.GetRequestsAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<AiRequestListItemDto>>.SuccessResponse(result));
    }

    [HttpGet("requests/{id:guid}")]
    public async Task<IActionResult> GetRequestDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await adminAiService.GetRequestDetailAsync(id, cancellationToken);
        return Ok(ApiResponse<AiRequestDetailDto>.SuccessResponse(result));
    }

    [HttpGet("feedback")]
    public async Task<IActionResult> GetFeedback(
        [FromQuery] AiFeedbackQuery query,
        CancellationToken cancellationToken)
    {
        var result = await adminAiService.GetFeedbackAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<AiFeedbackListItemDto>>.SuccessResponse(result));
    }

    [HttpPatch("feedback/{id:guid}")]
    public async Task<IActionResult> UpdateFeedback(
        Guid id,
        [FromBody] UpdateAiFeedbackDto dto,
        CancellationToken cancellationToken)
    {
        await adminAiService.UpdateFeedbackAsync(id, dto, cancellationToken);
        return Ok(ApiResponseBase.SuccessResponse("Feedback status updated."));
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await adminAiService.GetPerformanceAsync(days, cancellationToken);
        return Ok(ApiResponse<AiPerformanceDto>.SuccessResponse(result));
    }

    [HttpGet("insights")]
    public async Task<IActionResult> GetInsights(CancellationToken cancellationToken)
    {
        var result = await adminAiService.GetInsightsAsync(cancellationToken);
        return Ok(ApiResponse<AiInsightsDto>.SuccessResponse(result));
    }
}
