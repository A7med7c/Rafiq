using Rafiq.Domain.Common;

public abstract class MedicalDocument : BaseEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; protected set; }

    public Guid DocumentTypeId { get; protected set; }

    public string Title { get; protected set; } = null!;

    public string? Description { get; protected set; }

    public string? ImageUrl { get; protected set; } = null!;

    public byte[] ImageData { get; set; } = null!;
    public string? OCRText { get; protected set; }

    // Navigation
    public DocumentType DocumentType { get; private set; } = null!;
}