using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.Documents;

public class UserMedicine : BaseEntity
{
    // Required by EF Core
    protected UserMedicine() { }

    public UserMedicine(
        Guid userId,
        string medicineName,
        string dosage,
        string frequency,
        string duration,
        string? notes,
        string? imagePath,
        MedicineSource source)
    {
        UserId = userId;
        MedicineName = medicineName;
        Dosage = dosage;
        Frequency = frequency;
        Duration = duration;
        Notes = notes;
        ImagePath = imagePath;
        Source = source;
    }

    public Guid UserId { get; private set; }

    public string MedicineName { get; set; } = null!;

    public string Dosage { get; set; } = null!;

    public string Frequency { get; set; } = null!;

    public string Duration { get; set; } = null!;

    public string? Notes { get; set; }

    public string? ImagePath { get; private set; }

    public MedicineSource Source { get; private set; }
}
