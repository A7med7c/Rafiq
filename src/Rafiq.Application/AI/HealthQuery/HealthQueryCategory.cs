namespace Rafiq.Application.AI.HealthQuery;

/// <summary>
/// The fixed allowlist of health-data categories the assistant is permitted to retrieve.
/// The AI model may only ever request these values; anything else is dropped by
/// <see cref="HealthQueryIntentParser"/> before any repository is touched.
/// </summary>
public enum HealthQueryCategory
{
    Profile,
    Allergies,
    ChronicDiseases,
    Medicines,
    Appointments,
    LabReports,
    Prescriptions,
    ImagingReports,

    /// <summary>Reminder schedules for registered medicines (times, next/last/missed).</summary>
    MedicationReminders,

    /// <summary>
    /// Cross-family summary. Use ONLY when the question spans multiple family members
    /// without naming a specific person (e.g. "who has the most medications?").
    /// </summary>
    FamilyOverview,

    /// <summary>Uploaded general medical documents (not prescriptions, lab, or imaging reports).</summary>
    GeneralDocuments,

    /// <summary>Emergency contacts saved by the user (name, phone, relation).</summary>
    EmergencyContacts
}
