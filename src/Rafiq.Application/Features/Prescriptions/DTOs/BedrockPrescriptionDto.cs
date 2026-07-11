namespace Rafiq.Application.Features.Prescriptions.DTOs;

/// <summary>
/// The shape of the JSON that Bedrock returns for a prescription.
/// Property names match the JSON keys defined in PrescriptionPrompt.
/// </summary>
public sealed class BedrockPrescriptionDto
{
    public string? DoctorName { get; set; }

    public string? PatientName { get; set; }

    public string? PrescriptionDate { get; set; }

    public List<BedrockPrescriptionMedicineDto> Medicines { get; set; } = new();
}
