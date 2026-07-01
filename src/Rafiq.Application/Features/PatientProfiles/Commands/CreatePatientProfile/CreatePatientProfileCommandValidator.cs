using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CreatePatientProfile;

internal sealed class CreatePatientProfileCommandValidator : AbstractValidator<CreatePatientProfileCommand>
{
    public CreatePatientProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateOfBirth).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Gender).NotEmpty().Must(x => Enum.TryParse<Domain.Enums.Gender>(x, out _));
        RuleFor(x => x.BloodType).Must(x => string.IsNullOrWhiteSpace(x) || Enum.TryParse<Domain.Enums.BloodType>(x, out _))
            .WithMessage("BloodType must be a valid value.");
        RuleFor(x => x.EmergencyContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EmergencyContactPhone).NotEmpty().Matches(@"^01[0125][0-9]{8}$").WithMessage("Phone number must be a valid Egyptian mobile number.");
        RuleFor(x => x.Allergies).MaximumLength(2000);
        RuleFor(x => x.ChronicConditions).MaximumLength(2000);
    }
}
