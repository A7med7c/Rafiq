using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities;

public class Medication : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PrescribedBy { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;
    public Document? SourceDocument { get; set; }
    public ICollection<MedicationSchedule> MedicationSchedules { get; set; } = new List<MedicationSchedule>();
}
