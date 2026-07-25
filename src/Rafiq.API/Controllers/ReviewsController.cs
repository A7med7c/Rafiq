using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.Reviews.Commands.DeleteReview;
using Rafiq.Application.Features.Reviews.Commands.SubmitReview;
using Rafiq.Application.Features.Reviews.Commands.ToggleReviewVisibility;
using Rafiq.Application.Features.Reviews.Queries.GetAdminReviews;
using Rafiq.Application.Features.Reviews.Queries.GetPublicReviews;
using Rafiq.Application.Features.Reviews.Queries.GetReviewStats;

namespace Rafiq.API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitReview(
        [FromBody] SubmitReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SubmitReviewCommand(request.Stars, request.Comment),
            cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
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

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAdminReviewsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReviewStatsQuery(), cancellationToken);
        return Ok(result);
    }

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
}

public sealed class SubmitReviewRequest
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
}

public sealed class ToggleVisibilityBody
{
    public bool IsVisible { get; set; }
}
