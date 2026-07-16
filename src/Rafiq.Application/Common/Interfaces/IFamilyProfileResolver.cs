using Rafiq.Application.AI.Resolution;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Common.Interfaces;

public interface IFamilyProfileResolver
{
    ProfileResolution MatchByRelationship(RelationshipType relationship, IReadOnlyList<AccessibleProfileEntry> profiles);
    ProfileResolution MatchByNameInMessage(string messageText, IReadOnlyList<AccessibleProfileEntry> profiles);
    ProfileResolution MatchByHint(string hint, IReadOnlyList<AccessibleProfileEntry> profiles);
}
