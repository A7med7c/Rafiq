namespace Rafiq.Application.Features.Prescriptions.DTOs;

/// <summary>
/// Represents a single medicine as returned by Bedrock.
/// </summary>
public sealed class BedrockPrescriptionMedicineDto
{
    public string? MedicineName { get; set; }

    public string? Dosage { get; set; }

    public string? Frequency { get; set; }

    public string? Duration { get; set; }

    public string? Notes { get; set; }
}
