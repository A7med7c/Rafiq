using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Prescriptions.Queries.GetPrescriptionById;

public sealed class GetPrescriptionByIdQueryHandler(
    IHealthProfileAuthorizationService authorizationService,
    IPrescriptionRepository prescriptionRepository,
    IMedicalWarningCalculator warningCalculator)
    : IRequestHandler<GetPrescriptionByIdQuery, ApiResponse<PrescriptionResponseDto>>
{
    public async Task<ApiResponse<PrescriptionResponseDto>> Handle(
        GetPrescriptionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var prescription = await prescriptionRepository
            .GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.Prescription), request.Id);

        await authorizationService.EnsureCanReadAsync(prescription.UserHealthProfileId, cancellationToken);

        var dto = new PrescriptionResponseDto
        {
            Id = prescription.Id,
            DoctorName = prescription.DoctorName,
            PatientName = prescription.PatientName,
            PrescriptionDate = prescription.PrescriptionDate.ToString("yyyy-MM-dd"),
            ImagePath = prescription.ImagePath,
            CreatedAt = prescription.CreatedAt,
            MedicalAttentionReason = prescription.MedicalAttentionReason,
            RecommendedSpecialty = prescription.RecommendedSpecialty,
            ConfidenceScore = prescription.ConfidenceScore,
            RequiresMedicalAttention = warningCalculator.RequiresMedicalAttention(prescription.ConfidenceScore),
            AttentionLevel = warningCalculator.ComputeAttentionLevel(prescription.ConfidenceScore).ToString(),

            Medicines = prescription.Medicines.Select(m => new PrescriptionMedicineResponseDto
            {
                Id = m.Id,
                MedicineName = m.MedicineName,
                Dosage = m.Dosage,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Notes = m.Notes
            }).ToList()
        };

        return ApiResponse<PrescriptionResponseDto>.SuccessResponse(
            dto,
            "Prescription retrieved successfully.");
    }
}
