using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    IOtpService otpService,
    IFileStorageService fileStorageService)
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

        string? profileImageUrl = null;

        if (request.ProfileImage is not null)
        {
            var extension = Path.GetExtension(request.ProfileImage.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";

            using var imageStream = request.ProfileImage.OpenReadStream();

            profileImageUrl = await fileStorageService.UploadFileAsync(
                imageStream,
                fileName,
                "profile-images",
                cancellationToken);
        }

        var user = await identityService.CreateUserAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            profileImageUrl,
            cancellationToken);

        await otpService.SendOtpAsync(
            user.UserId,
            user.Email,
            OtpPurpose.EmailVerification,
            cancellationToken);

        return ApiResponse<RegisterResponseDto>.SuccessResponse(
            user,
            "Registration successful. Please verify your email.");
    }
}