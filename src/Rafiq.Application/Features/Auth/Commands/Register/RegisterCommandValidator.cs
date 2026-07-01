using FluentValidation;

namespace Rafiq.Application.Features.Auth.Commands.Register;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20).Matches(@"^\+[1-9]\d{1,14}$");
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^\da-zA-Z]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Confirm password must match password.");
        RuleFor(x => x.Role).Must(role => role is "Patient" or "Caregiver").WithMessage("Role must be Patient or Caregiver.");
    }
}
