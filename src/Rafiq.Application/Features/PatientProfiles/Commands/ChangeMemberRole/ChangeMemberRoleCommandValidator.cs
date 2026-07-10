using FluentValidation;

namespace Rafiq.Application.Features.PatientProfiles.Commands.ChangeMemberRole;

internal sealed class ChangeMemberRoleCommandValidator
    : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleCommandValidator()
    {
        RuleFor(x => x.ProfileId)
            .NotEmpty();

        RuleFor(x => x.AccessId)
            .NotEmpty();

        RuleFor(x => x.Role)
            .IsInEnum();
    }
}
