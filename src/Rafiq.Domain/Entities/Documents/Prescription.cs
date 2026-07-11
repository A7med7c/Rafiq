using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Entities.Documents;

public class Prescription : BaseEntity
{
    // Required by EF Core for materialisation
    protected Prescription() { }

    public Prescription(
        Guid userHealthProfileId,
        string doctorName,
        string patientName,
        DateOnly prescriptionDate,
        string imagePath)
    {
        UserHealthProfileId = userHealthProfileId;
        DoctorName = doctorName;
        PatientName = patientName;
        PrescriptionDate = prescriptionDate;
        ImagePath = imagePath;
    }

    public Guid UserHealthProfileId { get; private set; }

    public UserHealthProfile UserHealthProfile { get; private set; } = null!;

    public string DoctorName { get; set; } = null!;

    public string PatientName { get; set; } = null!;

    public DateOnly PrescriptionDate { get; set; }

    public string ImagePath { get; private set; } = null!;

    public ICollection<PrescriptionMedicine> Medicines { get; set; }
        = new List<PrescriptionMedicine>();
}
