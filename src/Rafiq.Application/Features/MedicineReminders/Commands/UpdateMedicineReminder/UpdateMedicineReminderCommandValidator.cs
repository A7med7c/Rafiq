using FluentValidation;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.MedicineReminders.Commands.UpdateMedicineReminder;

public class UpdateMedicineReminderCommandValidator : AbstractValidator<UpdateMedicineReminderCommand>
{
    public UpdateMedicineReminderCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(v => v.StartDate)
            .NotEmpty().WithMessage("StartDate is required.");

        RuleFor(v => v.EndDate)
            .NotEmpty().WithMessage("EndDate is required.")
            .GreaterThanOrEqualTo(v => v.StartDate)
            .WithMessage("EndDate must be greater than or equal to StartDate.");

        RuleFor(v => v.RepeatType)
            .IsInEnum().WithMessage("RepeatType must be a valid value.");

        When(v => v.RepeatType == RepeatType.Once, () =>
        {
            RuleFor(v => v.EndDate)
                .Equal(v => v.StartDate)
                .WithMessage("For 'Once' reminders, EndDate must be equal to StartDate.");
        });
    }
}
