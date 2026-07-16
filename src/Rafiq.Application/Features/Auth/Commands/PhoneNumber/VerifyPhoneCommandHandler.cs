using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Features.Auth.Commands.PhoneNumber
{
    public sealed class VerifyPhoneCommandHandler(
      IIdentityService identityService,
      IOtpService otpService)
      : IRequestHandler<VerifyPhoneCommand, ApiResponseBase>
    {
        public async Task<ApiResponseBase> Handle(
     VerifyPhoneCommand request,
     CancellationToken cancellationToken)
        {
            var user = await identityService.GetByEmailAsync(
                request.Email,
                cancellationToken);

            if (user is null)
                throw new NotFoundException("ApplicationUser", request.Email);

            if (user.EmailConfirmed)
                throw new ConflictException("Email already verified.");

            await otpService.VerifyOtpAsync(
                user.UserId,
                request.Code,
                OtpPurpose.EmailVerification,
                cancellationToken);

            await identityService.ConfirmEmailAsync(
                user.UserId,
                cancellationToken);

            return ApiResponseBase.SuccessResponse(
                "Email verified successfully.");
        }
    }
}
