using System;
using System.Text.Json;
namespace Rafiq.Application.Features.Common.DTOs;

/// <summary>
/// Generic DTO used for offline synchronization of future reminders.
/// Supports multiple reminder types (Medication, Appointment, etc.).
/// Contains only the data required by the mobile client to schedule a native notification.
/// </summary>
public sealed record UpcomingReminderDto
{
    /// <summary>
    /// Unique identifier of the reminder (backend primary key).
    /// </summary>
    public Guid ReminderId { get; init; }

    /// <summary>
    /// Type of the reminder (e.g., Medication, Appointment, ...).
    /// </summary>
    public ReminderType ReminderType { get; init; }

    /// <summary>
    /// Identifier of the underlying entity (e.g., MedicineId, AppointmentId).
    /// </summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// Title text for the local notification.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Body text for the local notification.
    /// </summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the reminder should fire, preserving offset.
    /// </summary>
    public DateTimeOffset ScheduledAt { get; init; }

    /// <summary>
    /// Status of the reminder (e.g., Pending, Sent, Confirmed, Skipped, Snoozed, Overdue, Missed).
    /// </summary>
    public string Status { get; init; } = string.Empty;


    /// <summary>
    /// Timestamp of the last update to this reminder on the server.
    /// This is the primary field for incremental synchronization.
    /// Mobile clients should store the highest UpdatedAt seen and use it
    /// as the cursor for subsequent sync requests.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Flag indicating the reminder has been deleted on the server (for incremental sync).
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    /// Optional payload containing additional metadata required for actions (Take, Snooze, Dismiss).
    /// </summary>
    public JsonElement? Payload { get; init; }
}
