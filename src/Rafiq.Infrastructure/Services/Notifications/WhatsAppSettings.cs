namespace Rafiq.Infrastructure.Services.Notifications;

public sealed class WhatsAppSettings
{
    public const string SectionName = "WhatsApp";

    public string AccessToken { get; set; } = string.Empty;

    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>Template sent to the patient at dose time (stage 2 reminder).</summary>
    public string PrimaryReminderTemplate { get; set; } = "medicine_now_reminder";

    /// <summary>Template sent to the patient and emergency contacts after a missed dose.</summary>
    public string EscalationTemplate { get; set; } = "emergency_reminder";
}
