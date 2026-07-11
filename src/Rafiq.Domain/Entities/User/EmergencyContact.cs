using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.User;

public class EmergencyContact : BaseEntity
{
    // Required by EF Core
    protected EmergencyContact() { }

    public EmergencyContact(
        Guid userId,
        string name,
        string phoneNumber,
        string relation)
    {
        UserId = userId;
        Name = name;
        PhoneNumber = phoneNumber;
        Relation = relation;
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = null!;

    public string PhoneNumber { get; private set; } = null!;

    public string Relation { get; private set; } = null!;
}
