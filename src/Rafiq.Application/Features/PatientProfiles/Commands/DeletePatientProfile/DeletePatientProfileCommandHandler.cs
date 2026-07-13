using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.DeletePatientProfile;

public sealed class DeletePatientProfileCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePatientProfileCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(DeletePatientProfileCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        // Only an active Owner of the profile may delete it.
        var access = await healthProfileAccessRepository.GetActiveOwnerAsync(
            request.PatientProfileId, currentUserId, cancellationToken)
            ?? throw new UnauthorizedException("You do not have permission to delete this profile.");

        var patientProfile = await patientProfileRepository.GetByIdAsync(
            request.PatientProfileId, cancellationToken)
            ?? throw new NotFoundException("PatientProfile", request.PatientProfileId);

        patientProfileRepository.Remove(patientProfile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(null, "Patient profile deleted successfully.");
    }
}
