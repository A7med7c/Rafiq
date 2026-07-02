using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.DeletePatientProfile;

public sealed class DeletePatientProfileCommandHandler(IPatientProfileRepository _patientProfileRepository, IUnitOfWork _unitOfWork) : IRequestHandler<DeletePatientProfileCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(DeletePatientProfileCommand request, CancellationToken cancellationToken)
    {
        var patientProfile = await _patientProfileRepository.GetByIdAsync(request.PatientProfileId, cancellationToken)
            ?? throw new NotFoundException("PatientProfile", request.PatientProfileId);

        _patientProfileRepository.Remove(patientProfile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(null, "Patient profile deleted successfully.");
    }
}
