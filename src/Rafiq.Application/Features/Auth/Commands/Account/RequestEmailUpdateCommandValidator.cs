using FluentValidation;

namespace Rafiq.Application.Features.Auth.Commands.Account;

public sealed class RequestEmailUpdateCommandValidator : AbstractValidator<RequestEmailUpdateCommand>
{
    public RequestEmailUpdateCommandValidator()
    {
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
