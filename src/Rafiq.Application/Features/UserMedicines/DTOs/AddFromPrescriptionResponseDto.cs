namespace Rafiq.Application.Features.UserMedicines.DTOs;

public sealed class AddFromPrescriptionResponseDto
{
    public int AddedCount { get; init; }
    public int SkippedCount { get; init; }
}
