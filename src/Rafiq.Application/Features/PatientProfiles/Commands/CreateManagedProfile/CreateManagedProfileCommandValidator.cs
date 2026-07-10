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
            .WithMessage("Date of birth cannot be in the future.");

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
            .IsInEnum().WithMessage("Relationship is required for a managed profile.")
            .NotEqual(RelationshipType.Self).WithMessage("Self cannot be used as the relationship for a managed profile.");

        RuleForEach(x => x.Allergies)
            .SetValidator(new CreateAllergyDtoValidator());

        RuleForEach(x => x.ChronicDiseases)
            .SetValidator(new CreateChronicDiseaseDtoValidator());
    }
}
