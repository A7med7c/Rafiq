using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;

public sealed record UpdatePatientProfileCommand(
    Guid PatientProfileId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    BloodType? BloodType,
    decimal? Height,
    decimal? Weight,
    List<UpdateAllergyDto> Allergies,
    List<UpdateChronicDiseaseDto> ChronicDiseases,
    RelationshipType? Relationship
) : IRequest<ApiResponse<PatientProfileDto>>
{
    public Guid? EntityId => PatientProfileId;
}