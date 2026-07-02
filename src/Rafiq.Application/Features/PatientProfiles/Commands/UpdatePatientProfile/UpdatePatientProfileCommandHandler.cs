using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;

public sealed class UpdatePatientProfileCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<UpdatePatientProfileCommand, ApiResponse<PatientProfileDto>>
{
    public async Task<ApiResponse<PatientProfileDto>> Handle(
        UpdatePatientProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await patientProfileRepository.GetByIdAsync(
            request.PatientProfileId,
            cancellationToken)
            ?? throw new NotFoundException("PatientProfile", request.PatientProfileId);

        profile.Update(
            request.Gender,
            request.DateOfBirth,
            request.Height,
            request.Weight,
            request.BloodType);

        profile.Allergies.Clear();

        foreach (var allergy in request.Allergies)
        {
            profile.Allergies.Add(new Allergy(
                allergy.Name,
                allergy.Severity));
        }

        profile.ChronicDiseases.Clear();

        foreach (var disease in request.ChronicDiseases)
        {
            profile.ChronicDiseases.Add(new ChronicDisease(
                disease.Name,
                disease.DiagnosedAt,
                disease.Status));
        }

        patientProfileRepository.Update(profile);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PatientProfileDto>.SuccessResponse(
            mapper.Map<PatientProfileDto>(profile),
            "Patient profile updated successfully.");
    }
}