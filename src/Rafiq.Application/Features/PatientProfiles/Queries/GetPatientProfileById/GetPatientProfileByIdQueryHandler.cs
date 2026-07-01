using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetPatientProfileById;

public sealed class GetPatientProfileByIdQueryHandler : IRequestHandler<GetPatientProfileByIdQuery, ApiResponse<PatientProfileDto>>
{
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IMapper _mapper;

    public GetPatientProfileByIdQueryHandler(IPatientProfileRepository patientProfileRepository, IMapper mapper)
    {
        _patientProfileRepository = patientProfileRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PatientProfileDto>> Handle(GetPatientProfileByIdQuery request, CancellationToken cancellationToken)
    {
        var patientProfile = await _patientProfileRepository.GetByIdAsync(request.PatientProfileId, cancellationToken)
            ?? throw new NotFoundException("PatientProfile", request.PatientProfileId);

        return ApiResponse<PatientProfileDto>.SuccessResponse(_mapper.Map<PatientProfileDto>(patientProfile));
    }
}
