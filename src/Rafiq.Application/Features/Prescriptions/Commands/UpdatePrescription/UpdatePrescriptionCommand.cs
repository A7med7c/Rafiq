using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;

namespace Rafiq.Application.Features.Prescriptions.Commands.UpdatePrescription;

/// <summary>
/// Updates the editable fields of a Prescription (DoctorName, PatientName, PrescriptionDate).
/// Only succeeds if the prescription belongs to the currently authenticated user.
/// The image and medicines are not updated through this command.
/// </summary>
public sealed record UpdatePrescriptionCommand(
    Guid Id,
    string DoctorName,
    string PatientName,
    string PrescriptionDate)
    : IRequest<ApiResponse<PrescriptionResponseDto>>;
