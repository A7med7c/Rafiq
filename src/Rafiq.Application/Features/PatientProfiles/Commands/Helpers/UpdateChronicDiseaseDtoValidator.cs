using FluentValidation;
using Rafiq.Application.Features.PatientProfiles.DTOs;

internal sealed class UpdateChronicDiseaseDtoValidator
    : AbstractValidator<UpdateChronicDiseaseDto>
{
    public UpdateChronicDiseaseDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}