using FluentValidation;

internal sealed class CreateAllergyDtoValidator
    : AbstractValidator<CreateAllergyDto>
{
    public CreateAllergyDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Severity)
            .IsInEnum();
    }
}