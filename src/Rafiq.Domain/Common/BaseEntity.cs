namespace Rafiq.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public DateTime? DeletedAt { get; protected set; }

    public virtual void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public virtual void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}