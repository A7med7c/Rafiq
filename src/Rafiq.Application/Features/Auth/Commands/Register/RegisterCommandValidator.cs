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
                .WithMessage("Validation.ProfileImageMustBeAJPEGPNGWEBP");

            RuleFor(x => x.ProfileImage!.Length)
                .LessThanOrEqualTo(MaxProfileImageSizeBytes)
                .WithMessage("Validation.ProfileImageMustNotExceed5MB");
        });

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^01[0125][0-9]{8}$").WithMessage("Validation.PhoneNumberMustBeAValidEgyptia2");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Validation.PasswordMustContainAtLeastOneU")
            .Matches("[a-z]").WithMessage("Validation.PasswordMustContainAtLeastOneL")
            .Matches(@"\d").WithMessage("Validation.PasswordMustContainAtLeastOneD")
            .Matches(@"[^\da-zA-Z]").WithMessage("Validation.PasswordMustContainAtLeastOneS");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Validation.ConfirmPasswordMustMatchPasswo");
    }
}
