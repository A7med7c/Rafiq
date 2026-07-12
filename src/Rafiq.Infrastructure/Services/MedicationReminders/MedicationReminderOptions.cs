namespace Rafiq.Infrastructure.Services.MedicationReminders;

public sealed class MedicationReminderOptions
{
    public const string SectionName = "MedicationReminders";

    /// <summary>
    /// Timezone that reminder wall-clock times are expressed in (IANA or Windows id, e.g. "Africa/Cairo").
    /// Defaults to UTC, which preserves the behaviour the system had before timezone handling was centralised.
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// How late a reminder may be and still fire immediately. Anything later than this is left for the
    /// next occurrence instead of firing the moment it is created — otherwise adding an 08:00 reminder
    /// at 20:00 would notify the user instantly.
    /// </summary>
    public int LateGraceMinutes { get; set; } = 5;
}
