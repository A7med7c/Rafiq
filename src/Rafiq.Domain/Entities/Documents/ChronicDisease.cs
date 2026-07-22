using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.User;

public class ChronicDisease : BaseEntity
{

    public ChronicDisease(string name, DateOnly? diagnosedAt, DiseaseStatus status)
    {
        Name = name;
        DiagnosedAt = diagnosedAt;
        Status = status;
    }
    public ChronicDisease() { }


    public Guid UserHealthProfileId { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly? DiagnosedAt { get; set; }

    public DiseaseStatus Status { get; set; }

    public UserHealthProfile UserHealthProfile { get; set; } = null!;

    public void Update(string name, DateOnly? diagnosedAt, DiseaseStatus status)
    {
        Name = name;
        DiagnosedAt = diagnosedAt;
        Status = status;
    }
}