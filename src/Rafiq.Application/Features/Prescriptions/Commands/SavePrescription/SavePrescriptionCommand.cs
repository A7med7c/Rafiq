using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;

namespace Rafiq.Application.Features.Prescriptions.Commands.SavePrescription;

public sealed record SavePrescriptionMedicineCommandItem(
    string? MedicineName,
    string? Dosage,
    string? Frequency,
    string? Duration,
    string? Instructions,
    string? Notes);

public sealed record SavePrescriptionCommand(
    Guid ProfileId,
    string? DoctorName,
    string? PatientName,
    string? PrescriptionDate,
    string? ImagePath,
    string? MedicalAttentionReason,
    string? RecommendedSpecialty,
    double? ConfidenceScore,
    List<SavePrescriptionMedicineCommandItem>? Medicines)
    : IRequest<ApiResponse<PrescriptionResponseDto>>;
