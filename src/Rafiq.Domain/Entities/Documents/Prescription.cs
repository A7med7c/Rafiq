using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.Documents;

public class Prescription : BaseEntity
{
    public string DoctorName { get; set; } = null!;

    public DateOnly VisitDate { get; set; }

    public string? Notes { get; set; }

    public ICollection<Medicine> Medicines { get; set; }
        = new List<Medicine>();
}