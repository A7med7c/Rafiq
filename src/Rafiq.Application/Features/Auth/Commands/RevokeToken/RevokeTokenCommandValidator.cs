using FluentValidation;

namespace Rafiq.Application.Features.Auth.Commands.RevokeToken;

internal sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
