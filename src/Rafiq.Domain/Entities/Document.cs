using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities;

public class Document : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public Guid? ProviderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public OcrStatus OcrStatus { get; set; }
    public DateTime? OcrCompletedAt { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;
    public HealthcareProvider? Provider { get; set; }
    public ICollection<ExtractedEntity> ExtractedEntities { get; set; } = new List<ExtractedEntity>();
    public ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
}
