using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities;

public class PatientProfile : BaseEntity
{
    private PatientProfile() { }

    public PatientProfile(
        string fullName,
        DateOnly dateOfBirth,
        Gender gender,
        BloodType? bloodType,
        string? allergies,
        string? chronicConditions,
        string emergencyContactName,
        string emergencyContactPhone,
        Guid? userId)
    {
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        BloodType = bloodType;
        Allergies = allergies;
        ChronicConditions = chronicConditions;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        UserId = userId;
    }

    public string FullName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public BloodType? BloodType { get; private set; }
    public string? Allergies { get; private set; }
    public string? ChronicConditions { get; private set; }
    public string EmergencyContactName { get; private set; } = string.Empty;
    public string EmergencyContactPhone { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }

    public ICollection<CaregiverLink> CaregiverLinks { get; private set; } = new List<CaregiverLink>();
    public ICollection<Document> Documents { get; private set; } = new List<Document>();
    public ICollection<Medication> Medications { get; private set; } = new List<Medication>();
    public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();
    public ICollection<LabResult> LabResults { get; private set; } = new List<LabResult>();
    public ICollection<ChatSession> ChatSessions { get; private set; } = new List<ChatSession>();
    public ICollection<Consent> Consents { get; private set; } = new List<Consent>();

    public void Update(
        string fullName,
        DateOnly dateOfBirth,
        Gender gender,
        BloodType? bloodType,
        string? allergies,
        string? chronicConditions,
        string emergencyContactName,
        string emergencyContactPhone)
    {
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        BloodType = bloodType;
        Allergies = allergies;
        ChronicConditions = chronicConditions;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        MarkUpdated();
    }
}
