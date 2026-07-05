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
        var user = await identityService.GetByPhoneAsync(
            request.PhoneNumber,
            cancellationToken);

        if (user is null)
            throw new NotFoundException("ApplicationUser", request.PhoneNumber);

        if (user.PhoneNumberConfirmed)
            throw new ConflictException("Phone number is already verified.");

        await otpService.SendOtpAsync(
            user.UserId,
            user.PhoneNumber,
            OtpPurpose.PhoneVerification,
            cancellationToken);

        return ApiResponseBase.SuccessResponse(
            "Verification code sent successfully.");
    }
}