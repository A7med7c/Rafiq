using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.Reviews.Commands.DeleteReview;
using Rafiq.Application.Features.Reviews.Commands.ReplyToReview;
using Rafiq.Application.Features.Reviews.Commands.SubmitReview;
using Rafiq.Application.Features.Reviews.Commands.ToggleReviewVisibility;
using Rafiq.Application.Features.Reviews.Commands.UpdateAdminNotes;
using Rafiq.Application.Features.Reviews.Commands.UpdateReviewCategory;
using Rafiq.Application.Features.Reviews.Commands.UpdateReviewStatus;
using Rafiq.Application.Features.Reviews.Queries.GetAdminReviews;
using Rafiq.Application.Features.Reviews.Queries.GetPublicReviews;
using Rafiq.Application.Features.Reviews.Queries.GetReviewOverview;
using Rafiq.Application.Features.Reviews.Queries.GetReviewStats;
using Rafiq.Application.Features.Reviews.Queries.GetReviewTrends;
using Rafiq.Domain.Entities;

namespace Rafiq.API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator) => _mediator = mediator;

    // ── Public ────────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitReview(
        [FromBody] SubmitReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SubmitReviewCommand(request.Stars, request.Comment),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicReviews(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPublicReviewsQuery(limit), cancellationToken);
        return Ok(result);
    }

    // ── Admin — read ──────────────────────────────────────────────────────────

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ReviewStatus? status = null,
        [FromQuery] ReviewCategory? category = null,
        [FromQuery] int? minStars = null,
        [FromQuery] int? maxStars = null,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAdminReviewsQuery(page, pageSize, status, category, minStars, maxStars, sortBy),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("overview")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReviewOverviewQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("trends")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTrends(
        [FromQuery] int months = 6,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetReviewTrendsQuery(months), cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReviewStatsQuery(), cancellationToken);
        return Ok(result);
    }

    // ── Admin — write ─────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteReviewCommand(id), cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPatch("{id:guid}/visibility")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleVisibility(
        Guid id, [FromBody] ToggleVisibilityBody body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ToggleReviewVisibilityCommand(id, body.IsVisible), cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateStatusBody body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateReviewStatusCommand(id, body.Status), cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPatch("{id:guid}/category")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(
        Guid id, [FromBody] UpdateCategoryBody body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateReviewCategoryCommand(id, body.Category), cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPatch("{id:guid}/notes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateNotes(
        Guid id, [FromBody] UpdateNotesBody body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateAdminNotesCommand(id, body.Notes), cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("{id:guid}/reply")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reply(
        Guid id, [FromBody] ReplyBody body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReplyToReviewCommand(id, body.Reply), cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}

public sealed class SubmitReviewRequest { public int Stars { get; set; } public string? Comment { get; set; } }
public sealed class ToggleVisibilityBody { public bool IsVisible { get; set; } }
public sealed class UpdateStatusBody { public ReviewStatus Status { get; set; } }
public sealed class UpdateCategoryBody { public ReviewCategory Category { get; set; } }
public sealed class UpdateNotesBody { public string? Notes { get; set; } }
public sealed class ReplyBody { public string? Reply { get; set; } }
