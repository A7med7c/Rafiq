using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.PhoneNumber;

public sealed class ResendPhoneCodeCommandHandler(
    IIdentityService identityService,
    IPhoneVerificationRepository phoneVerificationRepository,
    IOtpGenerator otpGenerator,
    IOtpHasher otpHasher,
    INotificationsService notificationsService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ResendPhoneCodeCommand, ApiResponseBase>
{
    public async Task<ApiResponseBase> Handle(
        ResendPhoneCodeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find user
        var user = await identityService.GetByPhoneAsync(
            request.PhoneNumber,
            cancellationToken);

        if (user is null)
            throw new NotFoundException("ApplicationUser", user.UserId);

        // 2. Already verified?
        if (user.PhoneNumberConfirmed)
            throw new ConflictException("Phone number is already verified.");

        // 3. Delete previous verification code
        await phoneVerificationRepository.DeleteByUserIdAsync(
            user.UserId,
            cancellationToken);

        // 4. Generate OTP
        var otp = otpGenerator.Generate();

        // 5. Hash OTP
        var hash = otpHasher.Hash(otp);

        // 6. Create verification entity
        var verification = PhoneVerification.Create(
            user.UserId,
            hash,
            DateTime.UtcNow.AddMinutes(5));

        // 7. Save
        var existing = await phoneVerificationRepository.GetLatestAsync(
     user.UserId,
     cancellationToken);

        if (existing is not null && !existing.CanResend())
        {
            throw new ValidationException(
                ["Please wait 60 seconds before requesting another code."]);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 8. Send SMS
        await notificationsService.SendSMSAsync(
            user.PhoneNumber,
            $"Your verification code is {otp}",
            cancellationToken);

        return ApiResponseBase.SuccessResponse(
            "Verification code sent successfully.");
    }
}