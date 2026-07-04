using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.PhoneNumber
{
    public sealed class VerifyPhoneCommandHandler(
     IPhoneVerificationRepository phoneVerificationRepository,
     IIdentityService identityService,
     IOtpHasher otpHasher,
     IUnitOfWork unitOfWork)
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
                throw new NotFoundException("ApplicationUser", user.UserId);

            if (user.PhoneNumberConfirmed)
                throw new ConflictException("Phone number already verified.");

            var verification = await phoneVerificationRepository.GetLatestAsync(
                user.UserId,
                cancellationToken) ??
                throw new NotFoundException("Verification code not found.", null);

            if (verification.IsUsed)
                throw new ConflictException("Verification code already used.");

            if (verification.IsExpired())
                throw new ValidationException(["Verification code has expired."]);

            if (!verification.CanVerify())
                throw new ValidationException(["Too many verification attempts."]);

            if (!otpHasher.Verify(request.Code, verification.CodeHash))
            {
                verification.IncrementAttempts();

                phoneVerificationRepository.Update(verification);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                throw new ValidationException(["Invalid verification code."]);
            }

            verification.MarkAsUsed();

            phoneVerificationRepository.Update(verification);

            await identityService.ConfirmPhoneNumberAsync(
                user.UserId,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponseBase.SuccessResponse(
                "Phone number verified successfully.");
        }
    }
}
