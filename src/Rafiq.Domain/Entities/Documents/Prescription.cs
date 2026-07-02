namespace Rafiq.Domain.Entities.Documents;

public class Prescription : MedicalDocument
{
    public string DoctorName { get; set; } = null!;

    public DateOnly VisitDate { get; set; }

    public string? Notes { get; set; }

    public ICollection<Medicine> Medicines { get; set; }
        = new List<Medicine>();
}