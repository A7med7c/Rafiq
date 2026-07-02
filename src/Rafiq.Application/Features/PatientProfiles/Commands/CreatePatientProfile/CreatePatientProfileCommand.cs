using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CreatePatientProfile;

public record CreatePatientProfileCommand(
    DateOnly DateOfBirth,
    Gender Gender,
    BloodType BloodType,
    decimal Height,
    decimal Weight,
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