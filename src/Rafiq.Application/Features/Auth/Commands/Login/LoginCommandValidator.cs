using FluentValidation;

namespace Rafiq.Application.Features.Auth.Commands.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.LoginIdentifier).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
