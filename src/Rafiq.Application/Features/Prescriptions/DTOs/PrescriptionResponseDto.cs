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

        public string? MedicalAttentionReason { get; init; }
    public string? RecommendedSpecialty { get; init; }
    public double? ConfidenceScore { get; init; }
    public bool RequiresMedicalAttention { get; init; }
    public string? AttentionLevel { get; init; }

    public DateTime CreatedAt { get; init; }

    public List<PrescriptionMedicineResponseDto> Medicines { get; init; } = new();
}
