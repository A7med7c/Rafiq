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
            var user = await identityService.GetByPhoneAsync(
                request.PhoneNumber,
                cancellationToken);

            if (user is null)
                throw new NotFoundException("ApplicationUser", request.PhoneNumber);

            if (user.PhoneNumberConfirmed)
                throw new ConflictException("Phone number already verified.");

            await otpService.VerifyOtpAsync(
                user.UserId,
                request.Code,
                OtpPurpose.PhoneVerification,
                cancellationToken);

            await identityService.ConfirmPhoneNumberAsync(
                user.UserId,
                cancellationToken);

            return ApiResponseBase.SuccessResponse(
                "Phone number verified successfully.");
        }
    }
}
