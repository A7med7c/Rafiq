using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.Reviews.Commands.SubmitReview;
using Rafiq.Application.Features.Reviews.Queries.GetPublicReviews;

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
}

public sealed class SubmitReviewRequest
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
}
