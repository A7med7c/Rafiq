using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.SendHealthProfileInvitation;

internal sealed class SendHealthProfileInvitationCommandValidator
    : AbstractValidator<SendHealthProfileInvitationCommand>
{
    public SendHealthProfileInvitationCommandValidator()
    {
        RuleFor(x => x.UserHealthProfileId)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Role)
            .IsInEnum();
    }
}
