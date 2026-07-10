using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CreateManagedProfile;

public sealed class CreateManagedProfileCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<CreateManagedProfileCommand, ApiResponse<PatientProfileDto>>
{
    public async Task<ApiResponse<PatientProfileDto>> Handle(
        CreateManagedProfileCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var profile = UserHealthProfile.CreateManaged(
            request.FirstName,
            request.LastName,
            request.Gender,
            request.DateOfBirth,
            request.Height,
            request.Weight,
            request.BloodType);

        foreach (var allergy in request.Allergies)
        {
            profile.Allergies.Add(new Allergy
            {
                Name = allergy.Name,
                Severity = allergy.Severity
            });
        }

        foreach (var disease in request.ChronicDiseases)
        {
            profile.ChronicDiseases.Add(new ChronicDisease
            {
                Name = disease.Name,
                DiagnosedAt = disease.DiagnosedAt,
                Status = disease.Status
            });
        }

        await patientProfileRepository.AddAsync(profile, cancellationToken);

        var ownerAccess = HealthProfileAccess.CreateManagedProfileOwner(profile.Id, currentUserId);

        await healthProfileAccessRepository.AddAsync(ownerAccess, cancellationToken);

        request.SetEntityId(profile.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PatientProfileDto>.SuccessResponse(
            mapper.Map<PatientProfileDto>(profile),
            "Managed profile created successfully.");
    }
}
