using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.Prescriptions.Commands.UpdatePrescription;

public sealed class UpdatePrescriptionCommandHandler(
    ICurrentUserService currentUserService,
    IPrescriptionRepository prescriptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePrescriptionCommand, ApiResponse<PrescriptionResponseDto>>
{
    public async Task<ApiResponse<PrescriptionResponseDto>> Handle(
        UpdatePrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        // The repository filters by both Id AND UserId — ownership is enforced at the DB level
        var prescription = await prescriptionRepository
            .GetByIdAsync(request.Id, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.Prescription), request.Id);

        // Update editable fields
        prescription.DoctorName = request.DoctorName;
        prescription.PatientName = request.PatientName;

        if (DateOnly.TryParseExact(
            request.PrescriptionDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed))
        {
            prescription.PrescriptionDate = parsed;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new PrescriptionResponseDto
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
        };

        return ApiResponse<PrescriptionResponseDto>.SuccessResponse(
            dto,
            "Prescription updated successfully.");
    }
}
