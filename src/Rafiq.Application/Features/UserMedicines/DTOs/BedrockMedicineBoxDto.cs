namespace Rafiq.Application.Features.UserMedicines.DTOs;

public sealed class BedrockMedicineBoxDto
{
    public bool IsValidDocument { get; set; }

    public bool IsUnreadable { get; set; }

    public string? DetectedDocumentType { get; set; }

    public string? MedicineName { get; set; }

    public string? Strength { get; set; }

    public string? DosageForm { get; set; }

    public string? Manufacturer { get; set; }
}
