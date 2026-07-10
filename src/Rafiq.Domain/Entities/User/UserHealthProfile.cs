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
        BloodType? bloodType)
    {
        return new UserHealthProfile(
            null,
            firstName,
            lastName,
            gender,
            dateOfBirth,
            height,
            weight,
            bloodType);
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

    public ICollection<Allergy> Allergies { get; set; }
        = new List<Allergy>();

    public ICollection<ChronicDisease> ChronicDiseases { get; set; }
        = new List<ChronicDisease>();

    public void Update(
        Gender gender,
        DateOnly dateOfBirth,
        decimal? height,
        decimal? weight,
        BloodType? bloodType)
    {
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Height = height;
        Weight = weight;
        BloodType = bloodType;

        MarkUpdated();
    }
}
