using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Prescriptions.Queries.GetMyPrescriptions;

public sealed class GetMyPrescriptionsQueryHandler(
    IHealthProfileAuthorizationService authorizationService,
    IPrescriptionRepository prescriptionRepository)
    : IRequestHandler<GetMyPrescriptionsQuery, ApiResponse<List<PrescriptionResponseDto>>>
{
    public async Task<ApiResponse<List<PrescriptionResponseDto>>> Handle(
        GetMyPrescriptionsQuery request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var prescriptions = await prescriptionRepository
            .GetAllByProfileIdAsync(request.ProfileId, cancellationToken);

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
