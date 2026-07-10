namespace Rafiq.Domain.Enums;

/// <summary>
/// How a HealthProfileAccess record came to exist. Distinguishes who is the decision
/// maker for a Pending record: for GrantInvitation it's the Grantee, for AccessRequest
/// it's the target profile's owner.
/// </summary>
public enum AccessOrigin
{
    /// <summary>Created directly by the system (Self Owner or Managed Profile Owner). Never Pending.</summary>
    Direct = 1,

    /// <summary>An Active Owner offered access to another user, who accepts or rejects.</summary>
    GrantInvitation = 2,

    /// <summary>A user requested access to another user's Self Profile; the profile owner accepts or rejects.</summary>
    AccessRequest = 3
}
