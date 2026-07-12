namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// Central clock and timezone authority.
///
/// Reminder wall-clock times (<c>MedicineReminder.ReminderTime</c>) are stored without a zone.
/// They are interpreted in <see cref="ReminderTimeZone"/> — the one place the application decides
/// what "08:00" means — and converted to UTC before any absolute-time calculation.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    /// <summary>Zone that reminder wall-clock times are expressed in.</summary>
    TimeZoneInfo ReminderTimeZone { get; }

    /// <summary>"Today" as seen by the user, i.e. in <see cref="ReminderTimeZone"/>.</summary>
    DateOnly Today { get; }

    /// <summary>
    /// Converts a reminder's local date + wall-clock time into the absolute UTC instant it occurs at.
    /// </summary>
    DateTime ToUtc(DateOnly localDate, TimeSpan localTime);
}
