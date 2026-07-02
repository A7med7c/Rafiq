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
    }
}