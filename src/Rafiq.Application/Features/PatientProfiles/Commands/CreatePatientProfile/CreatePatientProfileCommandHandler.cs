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
    IIdentityService identityService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
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

        var account = await identityService.GetAccountAsync(currentUserId, cancellationToken);

        var profile = UserHealthProfile.CreateSelf(
            currentUserId,
            account.FirstName,
            account.LastName,
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

        var selfOwnerAccess = HealthProfileAccess.CreateSelfOwner(profile.Id, currentUserId);

        await healthProfileAccessRepository.AddAsync(selfOwnerAccess, cancellationToken);

        request.SetEntityId(profile.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PatientProfileDto>.SuccessResponse(
            mapper.Map<PatientProfileDto>(profile),
            "Patient profile created successfully.");
    }
}