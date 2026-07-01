using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;

public sealed class UpdatePatientProfileCommandHandler : IRequestHandler<UpdatePatientProfileCommand, ApiResponse<PatientProfileDto>>
{
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdatePatientProfileCommandHandler(
        IPatientProfileRepository patientProfileRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _patientProfileRepository = patientProfileRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PatientProfileDto>> Handle(UpdatePatientProfileCommand request, CancellationToken cancellationToken)
    {
        var patientProfile = await _patientProfileRepository.GetByIdAsync(request.PatientProfileId, cancellationToken)
            ?? throw new NotFoundException("PatientProfile", request.PatientProfileId);

        patientProfile.Update(
            request.FullName,
            request.DateOfBirth,
            Enum.Parse<Gender>(request.Gender),
            string.IsNullOrWhiteSpace(request.BloodType) ? null : Enum.Parse<BloodType>(request.BloodType),
            request.Allergies,
            request.ChronicConditions,
            request.EmergencyContactName,
            request.EmergencyContactPhone);

        _patientProfileRepository.Update(patientProfile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PatientProfileDto>.SuccessResponse(
            _mapper.Map<PatientProfileDto>(patientProfile),
            "Patient profile updated successfully.");
    }
}
