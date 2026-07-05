namespace Rafiq.Application.Features.UserMedicines.DTOs;

public sealed class UserMedicineResponseDto
{
    public Guid Id { get; init; }

    public string MedicineName { get; init; } = null!;

    public string Dosage { get; init; } = null!;

    public string Frequency { get; init; } = null!;

    public string Duration { get; init; } = null!;

    public string? Notes { get; init; }

    public string? ImagePath { get; init; }

    public string Source { get; init; } = null!;

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
