using FluentValidation;

namespace Rafiq.Application.Features.Auth.Commands.Account;

public sealed class UpdateMyAccountCommandValidator : AbstractValidator<UpdateMyAccountCommand>
{
    public UpdateMyAccountCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^01[0125][0-9]{8}$")
            .WithMessage("Validation.PhoneNumberMustBeAValidEgyptia");
    }
}
