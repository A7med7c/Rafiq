using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Application.Features.MedicalReport.DTOs;

public sealed record MedicalReportDataDto(
    UserHealthProfile Profile,
    IReadOnlyList<UserMedicine> Medicines,
    IReadOnlyList<Prescription> Prescriptions,
    IReadOnlyList<LabReport> LabReports,
    IReadOnlyList<ImagingReport> ImagingReports,
    IReadOnlyList<Appointment> Appointments,
    IReadOnlyList<EmergencyContact> EmergencyContacts,
    string? AiClinicalSummary,
    ReportType ReportType,
    DateTime GeneratedAt);
