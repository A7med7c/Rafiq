using FluentValidation;

internal sealed class CreateChronicDiseaseDtoValidator
    : AbstractValidator<CreateChronicDiseaseDto>
{
    public CreateChronicDiseaseDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.DiagnosedAt)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Chronic disease diagnosis date cannot be in the future.")
            .When(x => x.DiagnosedAt.HasValue);
    }
}