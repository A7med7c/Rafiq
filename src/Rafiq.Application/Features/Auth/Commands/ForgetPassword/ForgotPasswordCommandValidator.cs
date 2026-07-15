using FluentValidation;

namespace Rafiq.Application.Features.Auth.Commands.ForgetPassword
{
    public sealed class ForgotPasswordCommandValidator
     : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("Invalid email address.");
        }
    }
}
