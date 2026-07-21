using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Features.Auth.Commands.Account;

public sealed class CancelEmailUpdateCommandHandler(
    ICurrentUserService currentUserService,
    IIdentityService identityService,
    IOtpService otpService)
    : IRequestHandler<CancelEmailUpdateCommand, ApiResponseBase>
{
    public async Task<ApiResponseBase> Handle(CancelEmailUpdateCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        var userId = currentUserService.UserId.Value;

        await identityService.CancelEmailUpdateAsync(userId, cancellationToken);
        await otpService.InvalidateOtpAsync(userId, OtpPurpose.EmailVerification, cancellationToken);

        return ApiResponseBase.SuccessResponse("Email change cancelled. Your previous email address has been restored.");
    }
}
