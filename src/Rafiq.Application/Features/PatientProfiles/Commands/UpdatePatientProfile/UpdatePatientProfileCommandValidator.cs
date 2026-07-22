using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;

internal sealed class UpdatePatientProfileCommandValidator
    : AbstractValidator<UpdatePatientProfileCommand>
{
    public UpdatePatientProfileCommandValidator()
    {
        RuleFor(x => x.PatientProfileId)
            .NotEmpty();

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
            .IsInEnum()
            .When(x => x.Relationship.HasValue);
    }
}
