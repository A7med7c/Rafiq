using FluentValidation;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CreateManagedProfile;

internal sealed class CreateManagedProfileCommandValidator
    : AbstractValidator<CreateManagedProfileCommand>
{
    public CreateManagedProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Validation.DateOfBirthCannotBeInTheFuture");

        RuleFor(x => x.Gender)
            .IsInEnum();

        RuleFor(x => x.BloodType)
            .IsInEnum()
            .When(x => x.BloodType.HasValue);

        RuleFor(x => x.Height)
            .InclusiveBetween(30m, 300m)
            .When(x => x.Height.HasValue);

        RuleFor(x => x.Weight)
            .InclusiveBetween(1m, 500m)
            .When(x => x.Weight.HasValue);

        RuleFor(x => x.Relationship)
            .IsInEnum().WithMessage("Validation.RelationshipIsRequiredForAMana")
            .NotEqual(RelationshipType.Self).WithMessage("Validation.SelfCannotBeUsedAsTheRelations");

        RuleForEach(x => x.Allergies)
            .SetValidator(new CreateAllergyDtoValidator());

        RuleForEach(x => x.ChronicDiseases)
            .SetValidator(new CreateChronicDiseaseDtoValidator());
    }
}
