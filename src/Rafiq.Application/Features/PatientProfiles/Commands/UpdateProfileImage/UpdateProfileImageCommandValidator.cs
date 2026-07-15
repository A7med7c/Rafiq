using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdateProfileImage;

internal sealed class UpdateProfileImageCommandValidator
    : AbstractValidator<UpdateProfileImageCommand>
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxProfileImageSizeBytes = 5 * 1024 * 1024;

    public UpdateProfileImageCommandValidator()
    {
        When(x => x.ProfileImage is not null, () =>
        {
            RuleFor(x => x.ProfileImage!.ContentType)
                .Must(contentType => AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
                .WithMessage("Profile image must be a JPEG, PNG, WEBP, or GIF file.");

            RuleFor(x => x.ProfileImage!.Length)
                .LessThanOrEqualTo(MaxProfileImageSizeBytes)
                .WithMessage("Profile image must not exceed 5 MB.");
        });
    }
}
