using FluentValidation;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.MedicineReminders.Commands.CreateMedicineReminders;

public class CreateMedicineRemindersCommandValidator : AbstractValidator<CreateMedicineRemindersCommand>
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateMedicineRemindersCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;

        RuleFor(v => v.UserMedicineId)
            .NotEmpty().WithMessage("Validation.UserMedicineIdIsRequired");

        RuleFor(v => v.StartDate)
            .NotEmpty().WithMessage("Validation.StartDateIsRequired")
            .GreaterThanOrEqualTo(_ => _dateTimeProvider.Today)
            .WithMessage("Validation.StartDateCannotBeBeforeTodaysD");

        RuleFor(v => v.EndDate)
            .NotEmpty().WithMessage("Validation.EndDateIsRequired")
            .GreaterThanOrEqualTo(v => v.StartDate)
            .WithMessage("Validation.EndDateMustBeGreaterThanOrEqua");

        RuleFor(v => v.RepeatType)
            .IsInEnum().WithMessage("Validation.RepeatTypeMustBeAValidValue");

        RuleFor(v => v.Times)
            .NotEmpty().WithMessage("Validation.AtLeastOneReminderTimeIsRequir")
            .Must(BeValidTimes).WithMessage("Validation.OneOrMoreReminderTimesHaveAnIn")
            .Must(NotContainDuplicates).WithMessage("Validation.DuplicateReminderTimesAreNotAl");

        When(v => v.RepeatType == RepeatType.Once, () =>
        {
            RuleFor(v => v.EndDate)
                .Equal(v => v.StartDate)
                .WithMessage("Validation.ForOnceRemindersEndDateMustBeE");

            RuleFor(v => v.Times)
                .Must((command, times) => BeFutureTimesIfToday(command.StartDate, times))
                .WithMessage("Validation.ForOnceRemindersStartingTodayT");
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

        var today = _dateTimeProvider.Today;
        if (startDate > today) return true;

        // Reminder times are wall-clock values in ReminderTimeZone, so "now" must be
        // converted into that same zone before comparing — not left as raw UTC.
        var now = TimeZoneInfo.ConvertTimeFromUtc(_dateTimeProvider.UtcNow, _dateTimeProvider.ReminderTimeZone).TimeOfDay;

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
