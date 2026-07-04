using FluentValidation;

namespace Rafiq.Application.Features.Auth.Commands.ExternalLogin
{
    public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
    {
        public GoogleLoginCommandValidator()
        {
            RuleFor(x => x.IdToken).NotEmpty();
        }
    }
}
