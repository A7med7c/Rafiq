using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CreatePatientProfile;

public sealed record CreatePatientProfileCommand(
    string FullName,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodType,
    string? Allergies,
    string? ChronicConditions,
    string EmergencyContactName,
    string EmergencyContactPhone,
    bool IsDependent = false) : IRequest<ApiResponse<PatientProfileDto>>, IAuditableRequest
{
    public AuditAction AuditAction => AuditAction.Create;
    public string EntityType => "PatientProfile";
    public Guid? EntityId { get; private set; }
    public Guid? PatientProfileId => EntityId;

    public void SetEntityId(Guid entityId)
    {
        EntityId = entityId;
    }
}
