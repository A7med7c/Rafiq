using Microsoft.AspNetCore.Identity;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Infrastructure.Persistence.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? ProfileImageUrl { get; set; }

    // The last confirmed email, kept while a new email is pending verification so it can be
    // restored if the user cancels. Null when there is no pending email change.
    public string? PreviousConfirmedEmail { get; set; }

    // Navigation Properties
    public UserHealthProfile? HealthProfile { get; set; }

    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
}