using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;

internal sealed class UpdatePatientProfileCommandValidator : AbstractValidator<UpdatePatientProfileCommand>
{
    public UpdatePatientProfileCommandValidator()
    {
        RuleFor(x => x.PatientProfileId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateOfBirth)
    .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow))
    .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.Gender).NotEmpty().Must(x => Enum.TryParse<Domain.Enums.Gender>(x, out _))
            .WithMessage("Gender must be a valid value.");
        RuleFor(x => x.BloodType).Must(x => string.IsNullOrWhiteSpace(x) || Enum.TryParse<Domain.Enums.BloodType>(x, out _))
            .WithMessage("BloodType must be a valid value.");
        RuleFor(x => x.EmergencyContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EmergencyContactPhone).NotEmpty().Matches(@"^01[0125][0-9]{8}$").WithMessage("Phone number must be a valid Egyptian mobile number.");
        RuleFor(x => x.Allergies).MaximumLength(2000);
        RuleFor(x => x.ChronicConditions).MaximumLength(2000);
    }
}
