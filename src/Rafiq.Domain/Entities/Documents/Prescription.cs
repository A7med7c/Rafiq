using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.Documents;

public class Prescription : BaseEntity
{
    // Required by EF Core for materialisation
    protected Prescription() { }

    public Prescription(
        Guid userId,
        string doctorName,
        string patientName,
        DateOnly prescriptionDate,
        string imagePath)
    {
        UserId = userId;
        DoctorName = doctorName;
        PatientName = patientName;
        PrescriptionDate = prescriptionDate;
        ImagePath = imagePath;
    }

    public Guid UserId { get; private set; }

    public string DoctorName { get; set; } = null!;

    public string PatientName { get; set; } = null!;

    public DateOnly PrescriptionDate { get; set; }

    public string ImagePath { get; private set; } = null!;

    public ICollection<PrescriptionMedicine> Medicines { get; set; }
        = new List<PrescriptionMedicine>();
}
