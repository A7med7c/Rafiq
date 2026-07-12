using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Infrastructure.Services.MedicationReminders;

namespace Rafiq.Infrastructure.Services.Common;

public sealed class DateTimeProvider : IDateTimeProvider
{
    private readonly TimeZoneInfo _reminderTimeZone;

    public DateTimeProvider(
        IOptions<MedicationReminderOptions> options,
        ILogger<DateTimeProvider> logger)
    {
        var configured = options.Value.TimeZone;

        try
        {
            _reminderTimeZone = TimeZoneInfo.FindSystemTimeZoneById(configured);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogError(
                ex,
                "Unknown reminder timezone '{TimeZone}'. Falling back to UTC.",
                configured);

            _reminderTimeZone = TimeZoneInfo.Utc;
        }
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public TimeZoneInfo ReminderTimeZone => _reminderTimeZone;

    public DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _reminderTimeZone));

    public DateTime ToUtc(DateOnly localDate, TimeSpan localTime)
    {
        var local = localDate.ToDateTime(TimeOnly.FromTimeSpan(localTime), DateTimeKind.Unspecified);

        // A DST spring-forward can make the wall-clock time non-existent; roll past the gap.
        if (_reminderTimeZone.IsInvalidTime(local))
            local = local.Add(_reminderTimeZone.GetAdjustmentRules().Length > 0
                ? TimeSpan.FromHours(1)
                : TimeSpan.Zero);

        return TimeZoneInfo.ConvertTimeToUtc(local, _reminderTimeZone);
    }
}
