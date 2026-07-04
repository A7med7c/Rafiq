using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    IOtpGenerator otpGenerator,
    IOtpHasher otpHasher,
    IPhoneVerificationRepository phoneVerificationRepository,
    IUnitOfWork unitOfWork,
    INotificationsService notificationsService)
    : IRequestHandler<RegisterCommand, ApiResponse<RegisterResponseDto>>
{
    public async Task<ApiResponse<RegisterResponseDto>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        if (await identityService.PhoneNumberExistsAsync(
                request.PhoneNumber,
                cancellationToken))
        {
            throw new ConflictException(
                "An account with this phone number already exists.");
        }

        var user = await identityService.CreateUserAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.Role,
            cancellationToken);

        // Generate OTP
        var otp = otpGenerator.Generate();

        var hashedOtp = otpHasher.Hash(otp);

        // Remove previous OTP
        await phoneVerificationRepository.DeleteByUserIdAsync(
            user.UserId,
            cancellationToken);

        // Create new verification

        var verification = PhoneVerification.Create(
            user.UserId,
            hashedOtp,
            DateTime.UtcNow.AddMinutes(5));

        await phoneVerificationRepository.AddAsync(
            verification,
            cancellationToken);

        // Save Database

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send SMS
        await notificationsService.SendSMSAsync(
            user.PhoneNumber,
            $"Your verification code is {otp}",
            cancellationToken);

        // Response

        return ApiResponse<RegisterResponseDto>.SuccessResponse(
            user,
            "Registration successful. Please verify your phone number.");
    }
}