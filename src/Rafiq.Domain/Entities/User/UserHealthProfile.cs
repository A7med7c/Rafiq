using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.User;

public class UserHealthProfile : BaseEntity
{
    private UserHealthProfile(
        Guid? userId,
        string firstName,
        string lastName,
        Gender gender,
        DateOnly dateOfBirth,
        decimal? height,
        decimal? weight,
        BloodType? bloodType)
    {
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Height = height;
        Weight = weight;
        BloodType = bloodType;
    }

    public UserHealthProfile() { }

    /// <summary>
    /// Creates a health profile for a registered account (Self Profile).
    /// </summary>
    public static UserHealthProfile CreateSelf(
        Guid userId,
        string firstName,
        string lastName,
        Gender gender,
        DateOnly dateOfBirth,
        decimal? height,
        decimal? weight,
        BloodType? bloodType)
    {
        return new UserHealthProfile(
            userId,
            firstName,
            lastName,
            gender,
            dateOfBirth,
            height,
            weight,
            bloodType);
    }

    /// <summary>
    /// Creates a health profile for someone without a registered account (Managed Profile).
    /// </summary>
    public static UserHealthProfile CreateManaged(
        string firstName,
        string lastName,
        Gender gender,
        DateOnly dateOfBirth,
        decimal? height,
        decimal? weight,
        BloodType? bloodType,
        string? profileImageUrl = null)
    {
        var profile = new UserHealthProfile(
            null,
            firstName,
            lastName,
            gender,
            dateOfBirth,
            height,
            weight,
            bloodType);

        profile.ProfileImageUrl = profileImageUrl;

        return profile;
    }

    /// <summary>
    /// Null when this is a Managed Profile for someone without a registered account.
    /// </summary>
    public Guid? UserId { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public Gender Gender { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public decimal? Height { get; set; }

    public decimal? Weight { get; set; }

    public BloodType? BloodType { get; set; }

    public string? ProfileImageUrl { get; private set; }

    public ICollection<Allergy> Allergies { get; set; }
        = new List<Allergy>();

    public ICollection<ChronicDisease> ChronicDiseases { get; set; }
        = new List<ChronicDisease>();

    public void Update(
        string firstName,
        string lastName,
        Gender gender,
        DateOnly dateOfBirth,
        decimal? height,
        decimal? weight,
        BloodType? bloodType)
    {
        FirstName = firstName;
        LastName = lastName;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Height = height;
        Weight = weight;
        BloodType = bloodType;
    }

    public void SetProfileImage(string? profileImageUrl)
    {
        ProfileImageUrl = profileImageUrl;
    }

    public void SyncAllergies(IEnumerable<Allergy> newAllergies)
    {
        // 1. Remove allergies not in the new list (matching by Name)
        var toRemove = Allergies
            .Where(a => !newAllergies.Any(na => string.Equals(na.Name, a.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var a in toRemove)
        {
            Allergies.Remove(a);
        }

        // 2. Add or update allergies
        foreach (var na in newAllergies)
        {
            var existing = Allergies.FirstOrDefault(a => string.Equals(a.Name, na.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Severity = na.Severity;
            }
            else
            {
                Allergies.Add(na);
            }
        }
    }

    public void SyncChronicDiseases(IEnumerable<ChronicDisease> newDiseases)
    {
        // 1. Remove chronic diseases not in the new list (matching by Name)
        var toRemove = ChronicDiseases
            .Where(d => !newDiseases.Any(nd => string.Equals(nd.Name, d.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var d in toRemove)
        {
            ChronicDiseases.Remove(d);
        }

        // 2. Add or update chronic diseases
        foreach (var nd in newDiseases)
        {
            var existing = ChronicDiseases.FirstOrDefault(d => string.Equals(d.Name, nd.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.DiagnosedAt = nd.DiagnosedAt;
                existing.Status = nd.Status;
            }
            else
            {
                ChronicDiseases.Add(nd);
            }
        }
    }
}
