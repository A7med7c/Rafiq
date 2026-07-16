using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.Auth.Commands.Account;
using Rafiq.Application.Features.Auth.Commands.ChangePassword;
using Rafiq.Application.Features.Auth.Commands.ExternalLogin;
using Rafiq.Application.Features.Auth.Commands.ForgetPassword;
using Rafiq.Application.Features.Auth.Commands.Login;
using Rafiq.Application.Features.Auth.Commands.Logout;
using Rafiq.Application.Features.Auth.Commands.PhoneNumber;
using Rafiq.Application.Features.Auth.Commands.RefreshToken;
using Rafiq.Application.Features.Auth.Commands.Register;
using Rafiq.Application.Features.Auth.Commands.ResetPassword;
using Rafiq.Application.Features.Auth.Commands.RevokeToken;
using Rafiq.Application.Features.Auth.Queries;

namespace Rafiq.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator _mediator) : ControllerBase
{

    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Register([FromForm] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyAccount(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyAccountQuery(), cancellationToken);
        return Ok(result);
    }
    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyAccount([FromBody] UpdateMyAccountCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("forget-password")]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("verify-reset-otp")]
    public async Task<IActionResult> VerifyResetOtp([FromBody] VerifyResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("verify-phone")]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("resend-phone-code")]
    public async Task<IActionResult> ResendPhoneCode([FromBody] ResendPhoneCodeCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command,
     CancellationToken cancellationToken)
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
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenCommand command,
     CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() }, cancellationToken);
        return Ok(result);
    }
}
