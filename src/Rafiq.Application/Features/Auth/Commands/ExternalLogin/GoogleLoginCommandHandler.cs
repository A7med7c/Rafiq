using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using System.Security.Authentication;

namespace Rafiq.Application.Features.Auth.Commands.ExternalLogin
{
    public sealed class GoogleLoginCommandHandler(
    IIdentityService identityService,
    ITokenIssuingService tokenIssuingService)
    : IRequestHandler<GoogleLoginCommand, ApiResponse<AuthResponseDto>>
    {
        public async Task<ApiResponse<AuthResponseDto>> Handle(
            GoogleLoginCommand request,
            CancellationToken cancellationToken)
        {
            var user = await identityService.LoginWithGoogleAsync(
                request.IdToken,
                cancellationToken);

            if (!user.PhoneNumberConfirmed)
                throw new AuthenticationException(
                    "Please verify your phone number before logging in.");

            var dto = await tokenIssuingService.IssueTokensAsync(user, cancellationToken);

            return ApiResponse<AuthResponseDto>.SuccessResponse(dto, "Google login successful.");
        }
    }
}
