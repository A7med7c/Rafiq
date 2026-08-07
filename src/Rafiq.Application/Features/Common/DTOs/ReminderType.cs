namespace Rafiq.Application.Features.Common.DTOs;

/// <summary>
/// Generic reminder type used by the offline synchronization contract.
/// Allows future extension to other reminder categories.
/// </summary>
public enum ReminderType
{
    Medication,
    Appointment
    // Add new types here as the product grows.
}
