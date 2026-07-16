using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Services.Notifications.EmailTemplates;

namespace Rafiq.Infrastructure.Services.auth
{
    internal sealed class OtpService(
    IOtpGenerator otpGenerator, IOtpHasher otpHasher, IOtpVerificationRepository otpRepository,
    IUnitOfWork unitOfWork, IEmailSender emailSender)
    : IOtpService
    {
        private const int OtpExpirationMinutes = 5;

        public async Task SendOtpAsync(
     Guid userId,
     string email,
     OtpPurpose purpose,
     CancellationToken cancellationToken)
        {
            // Check if there is an active OTP
            var existing = await otpRepository.GetLatestAsync(
                userId,
                purpose,
                cancellationToken);

            if (existing is not null)
            {
                // Prevent spam
                if (!existing.CanResend())
                {
                    throw new ValidationException("Please wait 60 seconds before requesting another verification code.");
                }

                // Delete previous OTP
                await otpRepository.DeleteByUserIdAsync(
                    userId,
                    purpose,
                    cancellationToken);
            }

            // Generate OTP
            var code = otpGenerator.Generate();

            // Hash OTP
            var hash = otpHasher.Hash(code);

            // Create entity
            var verification = OtpVerification.Create(
                userId,
                hash,
                purpose,
                DateTime.UtcNow.AddMinutes(OtpExpirationMinutes));

            // Save
            await otpRepository.AddAsync(
                verification,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send Email
            var subject = purpose == OtpPurpose.PasswordReset
                ? "Reset Your Password - Rafiq"
                : "Verify Your Email - Rafiq";

            var introText = purpose == OtpPurpose.PasswordReset
                ? "We received a request to reset your password. Use the verification code below to continue."
                : "Welcome to Rafiq! Thank you for creating your account. Please use the verification code below to activate your account.";

            var displayName = email.Contains('@') ? email[..email.IndexOf('@')] : email;

            var htmlBody = EmailTemplateBuilder.Layout(
                previewText: subject,
                bodyHtml: EmailTemplateBuilder.OtpBody(displayName, introText, code, OtpExpirationMinutes));

            await emailSender.SendEmailAsync(
                email,
                subject,
                htmlBody,
                cancellationToken);
        }

        public async Task<bool> VerifyOtpAsync(Guid userId, string otp, OtpPurpose purpose, CancellationToken cancellationToken)
        {
            var verification = await otpRepository.GetLatestAsync(
                    userId,
                    purpose,
                    cancellationToken)
                    ?? throw new ValidationException("OTP not found.");

            if (!verification.CanVerify())
            {
                throw new ValidationException(
                    "OTP has expired or maximum verification attempts have been reached.");
            }

            if (!otpHasher.Verify(otp, verification.CodeHash))
            {
                verification.IncrementAttempts();

                otpRepository.Update(verification);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                throw new ValidationException("Invalid OTP.");
            }

            verification.MarkAsUsed();

            otpRepository.Update(verification);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
