using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.Documents;

public class LabResult : BaseEntity
{
    public Guid LabReportId { get; set; }

    public string TestName { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public string NormalRange { get; set; } = null!;

    public LabReport LabReport { get; set; } = null!;
}