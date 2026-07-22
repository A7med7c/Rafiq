using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.Allergies.UpdateAllergy;

internal sealed class UpdateAllergyCommandValidator : AbstractValidator<UpdateAllergyCommand>
{
    public UpdateAllergyCommandValidator()
    {
        RuleFor(x => x.PatientProfileId)
            .NotEmpty();

        RuleFor(x => x.AllergyId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Severity)
            .IsInEnum();
    }
}
