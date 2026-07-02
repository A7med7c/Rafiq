using Rafiq.Domain.Common;

public abstract class MedicalDocument : BaseEntity
{
    public Guid UserId { get; private set; }

    public Guid DocumentTypeId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public string ImageUrl { get; private set; } = null!;

    public string? OCRText { get; private set; }

    // Navigation
    public DocumentType DocumentType { get; private set; } = null!;
}