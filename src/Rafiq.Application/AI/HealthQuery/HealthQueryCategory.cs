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
    ImagingReports
}
