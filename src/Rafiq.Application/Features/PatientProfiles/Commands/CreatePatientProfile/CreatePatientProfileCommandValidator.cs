using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CreatePatientProfile;

internal sealed class CreatePatientProfileCommandValidator
    : AbstractValidator<CreatePatientProfileCommand>
{
    public CreatePatientProfileCommandValidator()
    {
        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));

        RuleFor(x => x.Gender)
            .IsInEnum();

        RuleFor(x => x.BloodType)
            .IsInEnum();

        RuleFor(x => x.Height)
            .InclusiveBetween(30m, 300m);

        RuleFor(x => x.Weight)
            .InclusiveBetween(1m, 500m);

        RuleForEach(x => x.Allergies)
            .SetValidator(new CreateAllergyDtoValidator());

        RuleForEach(x => x.ChronicDiseases)
            .SetValidator(new CreateChronicDiseaseDtoValidator());
    }
}