using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IIdentityService identityService, ITokenIssuingService tokenIssuingService)
    : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
{
    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.ValidateCredentialsAsync(request.LoginIdentifier, request.Password, cancellationToken)
            ?? throw new AuthenticationException("Invalid email / phone number or password.");

        if (!user.PhoneNumberConfirmed)
            throw new AuthenticationException("Please verify your phone number before logging in.");

        var dto = await tokenIssuingService.IssueTokensAsync(user, cancellationToken);

        return ApiResponse<AuthResponseDto>.SuccessResponse(dto, "Login successful.");
    }
}