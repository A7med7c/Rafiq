using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Features.Auth.Commands.PhoneNumber;

public sealed class ResendPhoneCodeCommandHandler(
    IIdentityService identityService,
    IOtpService otpService)
    : IRequestHandler<ResendPhoneCodeCommand, ApiResponseBase>
{
    public async Task<ApiResponseBase> Handle(
     ResendPhoneCodeCommand request,
     CancellationToken cancellationToken)
    {
        var user = await identityService.GetByEmailAsync(
            request.Email,
            cancellationToken);

        if (user is null)
            throw new NotFoundException("ApplicationUser", request.Email);

        if (user.EmailConfirmed)
            throw new ConflictException("Email is already verified.");

        await otpService.SendOtpAsync(
            user.UserId,
            user.Email,
            OtpPurpose.EmailVerification,
            cancellationToken);

        return ApiResponseBase.SuccessResponse(
            "Verification code sent successfully.");
    }
}