namespace Rafiq.Application.Features.Prescriptions.DTOs;

/// <summary>
/// The client-facing response for a single medicine in a prescription.
/// </summary>
public sealed class PrescriptionMedicineResponseDto
{
    public Guid Id { get; init; }

    public string MedicineName { get; init; } = null!;

    public string? Dosage { get; init; }

    public string? Frequency { get; init; }

    public string? Duration { get; init; }

    public string? Notes { get; init; }
}
