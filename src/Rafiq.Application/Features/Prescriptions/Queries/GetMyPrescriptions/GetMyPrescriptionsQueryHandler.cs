using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Prescriptions.Queries.GetMyPrescriptions;

public sealed class GetMyPrescriptionsQueryHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IPrescriptionRepository prescriptionRepository)
    : IRequestHandler<GetMyPrescriptionsQuery, ApiResponse<List<PrescriptionResponseDto>>>
{
    public async Task<ApiResponse<List<PrescriptionResponseDto>>> Handle(
        GetMyPrescriptionsQuery request,
        CancellationToken cancellationToken)
    {
        var profileId = request.ProfileId;

        if (profileId == Guid.Empty)
        {
            var currentUserId = currentUserService.UserId
                ?? throw new UnauthorizedException("Authentication is required.");

            profileId = (await patientProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken))?.Id
                ?? throw new NotFoundException("PatientProfile", currentUserId);
        }

        await authorizationService.EnsureCanReadAsync(profileId, cancellationToken);

        var prescriptions = await prescriptionRepository
            .GetAllByProfileIdAsync(profileId, cancellationToken);

        var dtos = prescriptions.Select(prescription => new PrescriptionResponseDto
        {
            Id = prescription.Id,
            DoctorName = prescription.DoctorName,
            PatientName = prescription.PatientName,
            PrescriptionDate = prescription.PrescriptionDate.ToString("yyyy-MM-dd"),
            ImagePath = prescription.ImagePath,
            CreatedAt = prescription.CreatedAt,
            Medicines = prescription.Medicines.Select(m => new PrescriptionMedicineResponseDto
            {
                Id = m.Id,
                MedicineName = m.MedicineName,
                Dosage = m.Dosage,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Notes = m.Notes
            }).ToList()
        }).ToList();

        return ApiResponse<List<PrescriptionResponseDto>>.SuccessResponse(
            dtos,
            "Prescriptions retrieved successfully.");
    }
}
