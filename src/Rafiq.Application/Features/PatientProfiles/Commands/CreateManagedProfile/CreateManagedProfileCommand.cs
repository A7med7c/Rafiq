using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CreateManagedProfile;

public sealed record CreateManagedProfileCommand(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    BloodType? BloodType,
    decimal? Height,
    decimal? Weight,
    RelationshipType Relationship,
    List<CreateAllergyDto> Allergies,
    List<CreateChronicDiseaseDto> ChronicDiseases
) : IRequest<ApiResponse<PatientProfileDto>>
{
    public Guid? EntityId { get; private set; }

    public void SetEntityId(Guid id)
    {
        EntityId = id;
    }
}
