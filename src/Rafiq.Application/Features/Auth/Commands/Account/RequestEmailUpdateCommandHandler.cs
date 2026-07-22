using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Features.Auth.Commands.Account;

public sealed class RequestEmailUpdateCommandHandler(
    ICurrentUserService currentUserService,
    IIdentityService identityService,
    IOtpService otpService)
    : IRequestHandler<RequestEmailUpdateCommand, ApiResponseBase>
{
    public async Task<ApiResponseBase> Handle(RequestEmailUpdateCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        var userId = currentUserService.UserId.Value;
        var account = await identityService.GetAccountAsync(userId, cancellationToken);

        if (account.Email.Equals(request.NewEmail, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("New email must be different from your current email.");

        // Update email in identity store (sets EmailConfirmed = false) and send OTP to new address
        await identityService.UpdateEmailAsync(userId, request.NewEmail, cancellationToken);
        await otpService.SendOtpAsync(userId, request.NewEmail, OtpPurpose.EmailVerification, cancellationToken);

        return ApiResponseBase.SuccessResponse("A verification code has been sent to your new email address.");
    }
}
