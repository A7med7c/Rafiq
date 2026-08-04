using FluentValidation;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.MedicineReminders.Commands.UpdateMedicineReminder;

public class UpdateMedicineReminderCommandValidator : AbstractValidator<UpdateMedicineReminderCommand>
{
    public UpdateMedicineReminderCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Validation.IdIsRequired");

        RuleFor(v => v.StartDate)
            .NotEmpty().WithMessage("Validation.StartDateIsRequired");

        RuleFor(v => v.EndDate)
            .NotEmpty().WithMessage("Validation.EndDateIsRequired")
            .GreaterThanOrEqualTo(v => v.StartDate)
            .WithMessage("Validation.EndDateMustBeGreaterThanOrEqua");

        RuleFor(v => v.RepeatType)
            .IsInEnum().WithMessage("Validation.RepeatTypeMustBeAValidValue");

        When(v => v.RepeatType == RepeatType.Once, () =>
        {
            RuleFor(v => v.EndDate)
                .Equal(v => v.StartDate)
                .WithMessage("Validation.ForOnceRemindersEndDateMustBeE");
        });
    }
}
