using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetPatientProfileById;

public sealed class GetPatientProfileByIdQueryHandler(IPatientProfileRepository _patientProfileRepository, IMapper _mapper) : IRequestHandler<GetPatientProfileByIdQuery, ApiResponse<PatientProfileDto>>
{
    public async Task<ApiResponse<PatientProfileDto>> Handle(GetPatientProfileByIdQuery request, CancellationToken cancellationToken)
    {
        var patientProfile = await _patientProfileRepository.GetByIdAsync(request.PatientProfileId, cancellationToken)
            ?? throw new NotFoundException("PatientProfile", request.PatientProfileId);

        return ApiResponse<PatientProfileDto>.SuccessResponse(_mapper.Map<PatientProfileDto>(patientProfile));
    }
}
