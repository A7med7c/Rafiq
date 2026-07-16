using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.Auth.Commands.ForgetPassword
{
    public sealed class ForgotPasswordCommandHandler(IIdentityService _identityService, IOtpService otpService)
        : IRequestHandler<ForgotPasswordCommand, ApiResponseBase>
    {
        public async Task<ApiResponseBase> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var userDto = await _identityService.GetByEmailAsync(request.Email, cancellationToken);
            if (userDto is null)
                return ApiResponseBase.SuccessResponse();

            await otpService.SendOtpAsync(userDto.UserId, request.Email, OtpPurpose.PasswordReset, cancellationToken);
            return ApiResponseBase.SuccessResponse("Otp has been sent successfully");
        }
    }
}
