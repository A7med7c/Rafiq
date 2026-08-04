using FluentValidation;

namespace Rafiq.Application.Features.EmergencyContacts.Commands.CreateEmergencyContact;

internal sealed class CreateEmergencyContactCommandValidator : AbstractValidator<CreateEmergencyContactCommand>
{
    public CreateEmergencyContactCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.NameIsRequired")
            .MaximumLength(100).WithMessage("Validation.NameCannotExceed100Characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Validation.PhoneNumberIsRequired")
            .Matches(@"^01[0125][0-9]{8}$").WithMessage("Validation.PhoneNumberMustBeAValidEgyptia");

        RuleFor(x => x.Relation)
            .NotEmpty().WithMessage("Validation.RelationIsRequired")
            .MaximumLength(100).WithMessage("Validation.RelationCannotExceed100Charact");
    }
}
