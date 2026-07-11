namespace Rafiq.Application.Features.UserMedicines.DTOs;

public sealed class ScanMedicineBoxResponseDto
{
    public string? MedicineName { get; init; }

    public string? Strength { get; init; }

    public string? DosageForm { get; init; }

    public string? Manufacturer { get; init; }

    public string ImagePath { get; init; } = null!;
}
