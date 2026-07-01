using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CreatePatientProfile;

public sealed class CreatePatientProfileCommandHandler : IRequestHandler<CreatePatientProfileCommand, ApiResponse<PatientProfileDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreatePatientProfileCommandHandler(
        ICurrentUserService currentUserService,
        IPatientProfileRepository patientProfileRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _currentUserService = currentUserService;
        _patientProfileRepository = patientProfileRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PatientProfileDto>> Handle(CreatePatientProfileCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? throw new UnauthorizedException("Authentication is required.");

        if (!request.IsDependent && await _patientProfileRepository.ExistsByUserIdAsync(currentUserId, cancellationToken))
        {
            throw new ConflictException("A patient profile already exists for this user.");
        }

        var patientProfile = new PatientProfile(
            request.FullName,
            request.DateOfBirth,
            Enum.Parse<Gender>(request.Gender),
            string.IsNullOrWhiteSpace(request.BloodType) ? null : Enum.Parse<BloodType>(request.BloodType),
            request.Allergies,
            request.ChronicConditions,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.IsDependent ? null : currentUserId);

        await _patientProfileRepository.AddAsync(patientProfile, cancellationToken);
        request.SetEntityId(patientProfile.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PatientProfileDto>.SuccessResponse(
            _mapper.Map<PatientProfileDto>(patientProfile),
            "Patient profile created successfully.");
    }
}
