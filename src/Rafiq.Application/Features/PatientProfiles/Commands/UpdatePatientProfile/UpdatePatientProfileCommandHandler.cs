using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;

public sealed class UpdatePatientProfileCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    ICurrentUserService currentUserService,
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
            request.FirstName,
            request.LastName,
            request.Gender,
            request.DateOfBirth,
            request.Height,
            request.Weight,
            request.BloodType);

        if (request.Relationship.HasValue)
        {
            var currentUserId = currentUserService.UserId;
            if (currentUserId.HasValue)
            {
                var access = await healthProfileAccessRepository.GetActiveOwnerAsync(
                    request.PatientProfileId, currentUserId.Value, cancellationToken);

                if (access is not null && access.Relationship != RelationshipType.Self)
                    access.UpdateRelationship(request.Relationship.Value);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PatientProfileDto>.SuccessResponse(
            mapper.Map<PatientProfileDto>(profile),
            "Patient profile updated successfully.");
    }
}
