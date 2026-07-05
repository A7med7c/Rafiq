using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.Documents;

public class PrescriptionMedicine : BaseEntity
{
    public Guid PrescriptionId { get; set; }

    public string MedicineName { get; set; } = null!;

    public string Dosage { get; set; } = null!;

    public string Frequency { get; set; } = null!;

    public string Duration { get; set; } = null!;

    public string? Notes { get; set; }

    public Prescription Prescription { get; set; } = null!;
}
