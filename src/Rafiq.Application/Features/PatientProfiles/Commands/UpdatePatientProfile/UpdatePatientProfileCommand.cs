using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;

public sealed record UpdatePatientProfileCommand(
    Guid PatientProfileId,
    string FullName,
    DateTime DateOfBirth,
    string Gender,
    string? BloodType,
    string? Allergies,
    string? ChronicConditions,
    string EmergencyContactName,
    string EmergencyContactPhone)
    : IRequest<ApiResponse<PatientProfileDto>>, IPatientOwnedRequest, IAuditableRequest
{
    public AuditAction AuditAction => AuditAction.Update;
    public string EntityType => "PatientProfile";
    public Guid? EntityId => PatientProfileId;
    Guid? IAuditableRequest.PatientProfileId => PatientProfileId;
}
