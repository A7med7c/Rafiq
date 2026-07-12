using FluentValidation;

namespace Rafiq.Application.Features.MedicationReminderEngine.Commands.ConfirmMedicationReminder;

internal sealed class ConfirmMedicationReminderCommandValidator : AbstractValidator<ConfirmMedicationReminderCommand>
{
    public ConfirmMedicationReminderCommandValidator()
    {
        RuleFor(x => x.ReminderLogId)
            .NotEmpty().WithMessage("ReminderLogId is required.");
    }
}
