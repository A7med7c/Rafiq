using FluentValidation;

namespace Rafiq.Application.Features.Auth.Commands.ForgetPassword
{
    public sealed class ForgotPasswordCommandValidator
     : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Matches(@"^01[0125][0-9]{8}$")
                .WithMessage("Invalid Egyptian phone number.");
        }
    }
}
