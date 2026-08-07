using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.Documents;

public class UserMedicine : BaseEntity
{
    // Required by EF Core
    protected UserMedicine() { }

    public UserMedicine(
        Guid userHealthProfileId,
        string medicineName,
        string dosage,
        string frequency,
        string duration,
        string? notes,
        string? imagePath,
        MedicineSource source,
        string? medicalAttentionReason = null,
        string? recommendedSpecialty = null,
        double? confidenceScore = null,
        string? fileHash = null)
    {
        UserHealthProfileId = userHealthProfileId;
        MedicineName = medicineName;
        Dosage = dosage;
        Frequency = frequency;
        Duration = duration;
        Notes = notes;
        ImagePath = imagePath;
        Source = source;
        MedicalAttentionReason = medicalAttentionReason;
        RecommendedSpecialty = recommendedSpecialty;
        ConfidenceScore = confidenceScore;
        FileHash = fileHash;
    }

    public Guid UserHealthProfileId { get; private set; }

    public UserHealthProfile UserHealthProfile { get; private set; } = null!;

    public string MedicineName { get; set; } = null!;

    public string Dosage { get; set; } = null!;

    public string Frequency { get; set; } = null!;

    public string Duration { get; set; } = null!;

    public string? Notes { get; set; }

    public string? ImagePath { get; private set; }

    public MedicineSource Source { get; private set; }

    public string? FileHash { get; private set; }

    public string? MedicalAttentionReason { get; private set; }

    public string? RecommendedSpecialty { get; private set; }

    public double? ConfidenceScore { get; private set; }

    public virtual ICollection<MedicineReminder> Reminders { get; private set; } = new List<MedicineReminder>();
}
