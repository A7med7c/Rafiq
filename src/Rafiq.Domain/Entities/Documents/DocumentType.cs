using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.Documents;

public class DocumentType : BaseEntity
{
    // Required by EF Core for materialisation
    protected DocumentType() { }

    public DocumentType(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public ICollection<MedicalDocument> Documents { get; private set; }
        = new List<MedicalDocument>();
}