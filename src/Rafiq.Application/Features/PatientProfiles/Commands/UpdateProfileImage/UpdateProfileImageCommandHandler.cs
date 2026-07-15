using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdateProfileImage;

public sealed class UpdateProfileImageCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    IFileStorageService fileStorageService,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<UpdateProfileImageCommand, ApiResponse<PatientProfileDto>>
{
    public async Task<ApiResponse<PatientProfileDto>> Handle(
        UpdateProfileImageCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("PatientProfile", request.ProfileId);

        var previousImageUrl = profile.ProfileImageUrl;

        if (request.RemoveImage)
        {
            profile.SetProfileImage(null);
        }
        else if (request.ProfileImage is not null)
        {
            var extension = Path.GetExtension(request.ProfileImage.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";

            using var imageStream = request.ProfileImage.OpenReadStream();

            var imageUrl = await fileStorageService.UploadFileAsync(
                imageStream,
                fileName,
                "family-members",
                cancellationToken);

            profile.SetProfileImage(imageUrl);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousImageUrl) && previousImageUrl != profile.ProfileImageUrl)
        {
            await fileStorageService.DeleteFileAsync(previousImageUrl, cancellationToken);
        }

        return ApiResponse<PatientProfileDto>.SuccessResponse(
            mapper.Map<PatientProfileDto>(profile),
            "Profile image updated successfully.");
    }
}
