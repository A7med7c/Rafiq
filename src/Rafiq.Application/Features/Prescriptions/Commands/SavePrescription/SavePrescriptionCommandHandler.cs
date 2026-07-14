using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.Prescriptions.Commands.SavePrescription;

public sealed class SavePrescriptionCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IPrescriptionRepository prescriptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SavePrescriptionCommand, ApiResponse<PrescriptionResponseDto>>
{
    public async Task<ApiResponse<PrescriptionResponseDto>> Handle(
        SavePrescriptionCommand request,
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

        await authorizationService.EnsureCanWriteAsync(profileId, cancellationToken);

        var prescriptionDate = DateOnly.TryParseExact(
            request.PrescriptionDate, 
            "yyyy-MM-dd", 
            CultureInfo.InvariantCulture, 
            DateTimeStyles.None, 
            out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var doctorName = request.DoctorName ?? string.Empty;
        var patientName = request.PatientName ?? string.Empty;

        if (await prescriptionRepository.ExistsDuplicateAsync(profileId, prescriptionDate, doctorName, patientName, cancellationToken))
            throw new ConflictException("This document already exists in your medical records.");

        var prescription = new Prescription(
            profileId,
            doctorName,
            patientName,
            prescriptionDate,
            request.ImagePath ?? string.Empty);

        foreach (var medicine in request.Medicines ?? [])
        {
            prescription.Medicines.Add(new PrescriptionMedicine
            {
                MedicineName = medicine.MedicineName ?? string.Empty,
                Dosage = medicine.Dosage ?? string.Empty,
                Frequency = medicine.Frequency ?? string.Empty,
                Duration = medicine.Duration ?? string.Empty,
                Notes = medicine.Instructions ?? medicine.Notes
            });
        }

        await prescriptionRepository.AddAsync(prescription, cancellationToken);
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
            "Prescription saved successfully.");
    }
}
