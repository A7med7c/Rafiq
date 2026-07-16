using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.Allergies.CreateAllergy;

internal sealed class CreateAllergyCommandValidator : AbstractValidator<CreateAllergyCommand>
{
    public CreateAllergyCommandValidator()
    {
        RuleFor(x => x.PatientProfileId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Severity)
            .IsInEnum();
    }
}
