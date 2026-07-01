using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.Auth.Commands.Login;
using Rafiq.Application.Features.Auth.Commands.RefreshToken;
using Rafiq.Application.Features.Auth.Commands.Register;
using Rafiq.Application.Features.Auth.Commands.RevokeToken;

namespace Rafiq.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var enrichedCommand = command with
        {
            DeviceInfo = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        var result = await _mediator.Send(enrichedCommand, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var enrichedCommand = command with
        {
            DeviceInfo = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        var result = await _mediator.Send(enrichedCommand, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() }, cancellationToken);
        return Ok(result);
    }
}
