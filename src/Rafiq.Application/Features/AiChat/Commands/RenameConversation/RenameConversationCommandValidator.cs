using FluentValidation;

namespace Rafiq.Application.Features.AiChat.Commands.RenameConversation;

internal sealed class RenameConversationCommandValidator : AbstractValidator<RenameConversationCommand>
{
    public RenameConversationCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Validation.TitleCannotBeEmpty")
            .MaximumLength(255).WithMessage("Validation.TitleCannotExceed255Characters");
    }
}
