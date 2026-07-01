using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using UnauthorizedException = Rafiq.Domain.Exceptions.UnauthorizedException;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetMyPatientProfile;

public sealed class GetMyPatientProfileQueryHandler : IRequestHandler<GetMyPatientProfileQuery, ApiResponse<PatientProfileDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IMapper _mapper;

    public GetMyPatientProfileQueryHandler(
        ICurrentUserService currentUserService,
        IPatientProfileRepository patientProfileRepository,
        IMapper mapper)
    {
        _currentUserService = currentUserService;
        _patientProfileRepository = patientProfileRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PatientProfileDto>> Handle(GetMyPatientProfileQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var patientProfile = await _patientProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("PatientProfile", currentUserId);

        return ApiResponse<PatientProfileDto>.SuccessResponse(_mapper.Map<PatientProfileDto>(patientProfile));
    }
}
