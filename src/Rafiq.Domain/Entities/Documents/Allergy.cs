using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.User;

public class Allergy : BaseEntity
{

    public Allergy(string name, AllergySeverity severity)
    {
        Name = name;
        Severity = severity;
    }
    public Allergy()
    {

    }

    public Guid UserHealthProfileId { get; set; }

    public string Name { get; set; } = null!;

    public AllergySeverity Severity { get; set; }

    public UserHealthProfile UserHealthProfile { get; set; } = null!;
}