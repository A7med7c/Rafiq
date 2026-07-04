using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.User;

public class UserHealthProfile : BaseEntity
{

    public UserHealthProfile(
       Guid userId,
       Gender gender,
       DateOnly dateOfBirth,
       decimal height,
       decimal weight,
       BloodType bloodType)
    {
        UserId = userId;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Height = height;
        Weight = weight;
        BloodType = bloodType;
    }

    public UserHealthProfile() { }

    public Guid UserId { get; set; }

    public Gender Gender { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public decimal Height { get; set; }

    public decimal Weight { get; set; }

    public BloodType BloodType { get; set; }

    public ICollection<Allergy> Allergies { get; set; }
        = new List<Allergy>();

    public ICollection<ChronicDisease> ChronicDiseases { get; set; }
        = new List<ChronicDisease>();
    public void Update(
Gender gender,
DateOnly dateOfBirth,
decimal height,
decimal weight,
BloodType bloodType)
    {
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Height = height;
        Weight = weight;
        BloodType = bloodType;

        MarkUpdated();
    }
}