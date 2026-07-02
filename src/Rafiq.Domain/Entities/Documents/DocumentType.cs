using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.Documents;

public class DocumentType : BaseEntity
{
    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public ICollection<MedicalDocument> Documents { get; private set; }
        = new List<MedicalDocument>();
}