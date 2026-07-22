using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.ChronicDiseases.UpdateChronicDisease;

internal sealed class UpdateChronicDiseaseCommandValidator : AbstractValidator<UpdateChronicDiseaseCommand>
{
    public UpdateChronicDiseaseCommandValidator()
    {
        RuleFor(x => x.PatientProfileId)
            .NotEmpty();

        RuleFor(x => x.DiseaseId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.DiagnosedAt)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Diagnosed date cannot be in the future.")
            .When(x => x.DiagnosedAt.HasValue);
    }
}
