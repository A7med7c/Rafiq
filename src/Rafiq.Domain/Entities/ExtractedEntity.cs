using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities;

public class ExtractedEntity : BaseEntity
{
    public Guid DocumentId { get; set; }
    public ExtractedEntityType EntityType { get; set; }
    public string EntityValue { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public bool IsUserConfirmed { get; set; }
    public Document Document { get; set; } = null!;
}
