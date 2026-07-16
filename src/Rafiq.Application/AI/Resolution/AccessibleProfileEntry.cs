using Rafiq.Domain.Enums;

namespace Rafiq.Application.AI.Resolution;

public sealed record AccessibleProfileEntry(
    Guid ProfileId,
    string FirstName,
    string LastName,
    RelationshipType? Relationship);
