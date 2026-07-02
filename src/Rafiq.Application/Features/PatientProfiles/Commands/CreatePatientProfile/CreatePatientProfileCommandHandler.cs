using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CreatePatientProfile;

public sealed class CreatePatientProfileCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<CreatePatientProfileCommand, ApiResponse<PatientProfileDto>>
{
    public async Task<ApiResponse<PatientProfileDto>> Handle(
        CreatePatientProfileCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var profileExists = await patientProfileRepository
            .ExistsByUserIdAsync(currentUserId, cancellationToken);

        if (profileExists)
            throw new ConflictException("A patient profile already exists for this user.");

        var profile = new UserHealthProfile
        {
            UserId = currentUserId,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            Height = request.Height,
            Weight = request.Weight,
            BloodType = request.BloodType,
        };

        foreach (var allergy in request.Allergies)
        {
            profile.Allergies.Add(new Allergy
            {
                Name = allergy.Name,
                Severity = Enum.Parse<AllergySeverity>(allergy.Severity, true),
            });
        }

        foreach (var disease in request.ChronicDiseases)
        {
            profile.ChronicDiseases.Add(new ChronicDisease
            {
                Name = disease.Name,
                DiagnosedAt = disease.DiagnosedAt,
                Status = Enum.Parse<DiseaseStatus>(disease.Status, true)
            });
        }

        await patientProfileRepository.AddAsync(profile, cancellationToken);

        request.SetEntityId(profile.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PatientProfileDto>.SuccessResponse(
            mapper.Map<PatientProfileDto>(profile),
            "Patient profile created successfully.");
    }
}