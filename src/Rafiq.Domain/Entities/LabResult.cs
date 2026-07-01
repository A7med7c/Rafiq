using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities;

public class LabResult : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public Guid? DocumentId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string ResultValue { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public bool IsAbnormal { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;
    public Document? Document { get; set; }
}
