using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;

internal sealed class UpdatePatientProfileCommandValidator
    : AbstractValidator<UpdatePatientProfileCommand>
{
    public UpdatePatientProfileCommandValidator()
    {
        RuleFor(x => x.PatientProfileId)
            .NotEmpty();

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");

        RuleFor(x => x.Gender)
            .IsInEnum();

        RuleFor(x => x.BloodType)
            .IsInEnum();

        RuleFor(x => x.Height)
            .InclusiveBetween(30m, 300m);

        RuleFor(x => x.Weight)
            .InclusiveBetween(1m, 500m);

        RuleForEach(x => x.Allergies)
            .SetValidator(new UpdateAllergyDtoValidator());

        RuleForEach(x => x.ChronicDiseases)
            .SetValidator(new UpdateChronicDiseaseDtoValidator());
    }
}