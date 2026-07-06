using FluentValidation;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.MedicineReminders.Commands.CreateMedicineReminders;

public class CreateMedicineRemindersCommandValidator : AbstractValidator<CreateMedicineRemindersCommand>
{
    public CreateMedicineRemindersCommandValidator()
    {
        RuleFor(v => v.UserMedicineId)
            .NotEmpty().WithMessage("UserMedicineId is required.");

        RuleFor(v => v.StartDate)
            .NotEmpty().WithMessage("StartDate is required.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("StartDate cannot be before today's date.");

        RuleFor(v => v.EndDate)
            .NotEmpty().WithMessage("EndDate is required.")
            .GreaterThanOrEqualTo(v => v.StartDate)
            .WithMessage("EndDate must be greater than or equal to StartDate.");

        RuleFor(v => v.RepeatType)
            .IsInEnum().WithMessage("RepeatType must be a valid value.");

        RuleFor(v => v.Times)
            .NotEmpty().WithMessage("At least one reminder time is required.")
            .Must(BeValidTimes).WithMessage("One or more reminder times have an invalid format.")
            .Must(NotContainDuplicates).WithMessage("Duplicate reminder times are not allowed within the same request.");

        When(v => v.RepeatType == RepeatType.Once, () =>
        {
            RuleFor(v => v.EndDate)
                .Equal(v => v.StartDate)
                .WithMessage("For 'Once' reminders, EndDate must be equal to StartDate.");

            RuleFor(v => v.Times)
                .Must((command, times) => BeFutureTimesIfToday(command.StartDate, times))
                .WithMessage("For 'Once' reminders starting today, the time must be in the future.");
        });
    }

    private bool BeValidTimes(List<string> times)
    {
        if (times == null) return false;
        return times.All(t => TimeSpan.TryParse(t, out _));
    }

    private bool NotContainDuplicates(List<string> times)
    {
        if (times == null) return false;
        return times.Distinct().Count() == times.Count;
    }

    private bool BeFutureTimesIfToday(DateOnly startDate, List<string> times)
    {
        if (times == null) return true;
        
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (startDate > today) return true;

        var now = DateTime.UtcNow.TimeOfDay;
        
        foreach (var t in times)
        {
            if (TimeSpan.TryParse(t, out var parsedTime))
            {
                if (parsedTime <= now) return false;
            }
        }
        return true;
    }
}
