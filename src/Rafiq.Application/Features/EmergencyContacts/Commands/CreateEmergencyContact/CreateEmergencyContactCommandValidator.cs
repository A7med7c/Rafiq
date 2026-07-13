using FluentValidation;

namespace Rafiq.Application.Features.EmergencyContacts.Commands.CreateEmergencyContact;

internal sealed class CreateEmergencyContactCommandValidator : AbstractValidator<CreateEmergencyContactCommand>
{
    public CreateEmergencyContactCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^01[0125][0-9]{8}$").WithMessage("Phone number must be a valid Egyptian mobile number (e.g. 01012345678).");

        RuleFor(x => x.Relation)
            .NotEmpty().WithMessage("Relation is required.")
            .MaximumLength(100).WithMessage("Relation cannot exceed 100 characters.");
    }
}
