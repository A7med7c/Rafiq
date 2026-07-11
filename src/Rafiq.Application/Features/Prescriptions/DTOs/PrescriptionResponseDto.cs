namespace Rafiq.Application.Features.Prescriptions.DTOs;

/// <summary>
/// The client-facing response for a Prescription.
/// Returned by both the upload command and the query handlers.
/// </summary>
public sealed class PrescriptionResponseDto
{
    public Guid Id { get; init; }

    public string DoctorName { get; init; } = null!;

    public string PatientName { get; init; } = null!;

    public string? PrescriptionDate { get; init; }

    public string? ImagePath { get; init; } // Relative URL path

    public DateTime CreatedAt { get; init; }

    public List<PrescriptionMedicineResponseDto> Medicines { get; init; } = new();
}
