using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Features.Auth.Commands.ResetPassword
{
    public sealed class VerifyResetPasswordCommandHandler(IIdentityService _identityService, IOtpService _otpService,
        IResetTokenService _resetTokenService)
        : IRequestHandler<VerifyResetPasswordCommand, ApiResponse<VerifyResetOtpResponseDto>>
    {
        public async Task<ApiResponse<VerifyResetOtpResponseDto>> Handle(VerifyResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _identityService.GetByPhoneAsync(request.phoneNumber, cancellationToken)
               ?? throw new NotFoundException("user", request.phoneNumber);

            var verified = await _otpService.VerifyOtpAsync(user.UserId, request.code, OtpPurpose.PasswordReset, cancellationToken);

            if (!verified)
                throw new NotFoundException("OtpVerification", request.code);

            var resetToken = _resetTokenService.GenerateResetToken(user.UserId);

            return ApiResponse<VerifyResetOtpResponseDto>.SuccessResponse(new VerifyResetOtpResponseDto(resetToken),
                "OTP verified successfully.");
        }
    }
}
