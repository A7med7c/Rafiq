using FluentValidation;

namespace Rafiq.Application.Features.Auth.Commands.Register;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxProfileImageSizeBytes = 5 * 1024 * 1024;

    public RegisterCommandValidator()
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

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^01[0125][0-9]{8}$").WithMessage("Phone number must be a valid Egyptian mobile number.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^\da-zA-Z]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Confirm password must match password.");
    }
}
