using FluentValidation;

namespace Rafiq.Application.Features.AiChat.Commands.RenameConversation;

internal sealed class RenameConversationCommandValidator : AbstractValidator<RenameConversationCommand>
{
    public RenameConversationCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title cannot be empty.")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters.");
    }
}
