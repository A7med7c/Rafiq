using FluentValidation;
using Rafiq.Application.Features.PatientProfiles.DTOs;

internal sealed class UpdateAllergyDtoValidator
    : AbstractValidator<UpdateAllergyDto>
{
    public UpdateAllergyDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Severity)
            .IsInEnum();

        RuleFor(x => x.Reaction)
            .MaximumLength(500);

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}