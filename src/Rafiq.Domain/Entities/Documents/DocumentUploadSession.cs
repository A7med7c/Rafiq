using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.Documents;

public class DocumentUploadSession
{
    protected DocumentUploadSession() { }

    public DocumentUploadSession(
        Guid userId,
        string imageUrl,
        string fileHash,
        DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ImageUrl = imageUrl;
        FileHash = fileHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    
    public Guid UserId { get; private set; }

    public string ImageUrl { get; private set; } = null!;

    public string FileHash { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    
    public DateTime ExpiresAt { get; private set; }
}
