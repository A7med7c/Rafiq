using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.Documents;

public class Medicine : BaseEntity
{
    public Guid PrescriptionId { get; set; }

    public string DrugName { get; set; } = null!;

    public string Dose { get; set; } = null!;

    public string Frequency { get; set; } = null!;

    public string Duration { get; set; } = null!;

    public string? Instructions { get; set; }

    public Prescription Prescription { get; set; } = null!;

    public ICollection<MedicineReminder> Reminders { get; set; }
        = new List<MedicineReminder>();
}