using FluentValidation;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.RequestHealthProfileAccess;

internal sealed class RequestHealthProfileAccessCommandValidator
    : AbstractValidator<RequestHealthProfileAccessCommand>
{
    public RequestHealthProfileAccessCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Role)
            .IsInEnum();

        RuleFor(x => x.Role)
            .Must(role => role is AccessRole.Manager or AccessRole.Viewer)
            .WithMessage("Validation.OnlyManagerOrViewerAccessCanBe");
    }
}
