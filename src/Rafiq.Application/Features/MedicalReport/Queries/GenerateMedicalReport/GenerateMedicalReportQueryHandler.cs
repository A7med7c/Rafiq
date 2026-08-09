using MediatR;
using Rafiq.Application.AI.HealthQuery;
using Rafiq.Application.AI.Models;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicalReport.DTOs;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicalReport.Queries.GenerateMedicalReport;

public sealed class GenerateMedicalReportQueryHandler(
    IHealthProfileAuthorizationService authService,
    ICurrentUserService currentUserService,
    IPatientProfileRepository profileRepository,
    IUserMedicineRepository medicineRepository,
    IPrescriptionRepository prescriptionRepository,
    ILabReportRepository labReportRepository,
    IImagingReportRepository imagingReportRepository,
    IAppointmentRepository appointmentRepository,
    IEmergencyContactRepository emergencyContactRepository,
    IGeneralDocumentRepository generalDocumentRepository,
    IAiChatService aiChatService,
    IMedicalReportPdfGenerator pdfGenerator)
    : IRequestHandler<GenerateMedicalReportQuery, ApiResponse<byte[]>>
{
    private const string ClinicalSummaryPrompt =
        "You are Rafiq, a professional AI health assistant generating a clinical summary for a physician. " +
        "Based on the patient's health data provided, write a concise clinical summary in 150–250 words " +
        "covering: chief complaints/conditions, current medications, recent lab findings, imaging results, " +
        "and any notable upcoming follow-ups. Use formal medical language. Do not fabricate information. " +
        "If a section has no data, omit it entirely.";

    public async Task<ApiResponse<byte[]>> Handle(
        GenerateMedicalReportQuery request, CancellationToken cancellationToken)
    {
        await authService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var profile = await profileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new Domain.Exceptions.NotFoundException("UserHealthProfile", request.ProfileId);

        var userId = currentUserService.UserId
            ?? throw new Domain.Exceptions.UnauthorizedException("User not authenticated.");

        var medicines         = await medicineRepository.GetAllByProfileIdAsync(request.ProfileId, cancellationToken);
        var prescriptions     = await prescriptionRepository.GetAllByProfileIdAsync(request.ProfileId, cancellationToken);
        var labReports        = await labReportRepository.GetAllByProfileIdAsync(request.ProfileId, cancellationToken);
        var imagingReports    = await imagingReportRepository.GetAllByProfileIdAsync(request.ProfileId, cancellationToken);
        var appointments      = await appointmentRepository.GetAllByUserHealthProfileIdAsync(request.ProfileId, cancellationToken);
        var emergencyContacts = await emergencyContactRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var generalDocuments  = await generalDocumentRepository.GetAllByUserIdAsync(request.ProfileId, cancellationToken);

        var aiSummary = await TryGenerateAiSummaryAsync(
            profile, medicines, labReports, imagingReports, cancellationToken);

        var reportData = new MedicalReportDataDto(
            profile,
            medicines,
            prescriptions,
            labReports,
            imagingReports,
            appointments,
            emergencyContacts,
            generalDocuments,
            aiSummary,
            DateTime.UtcNow);

        var pdfBytes = pdfGenerator.Generate(reportData);
        return ApiResponse<byte[]>.SuccessResponse(pdfBytes);
    }

    private async Task<string?> TryGenerateAiSummaryAsync(
        Domain.Entities.User.UserHealthProfile profile,
        IReadOnlyList<Domain.Entities.Documents.UserMedicine> medicines,
        IReadOnlyList<Domain.Entities.Documents.LabReport> labReports,
        IReadOnlyList<Domain.Entities.Documents.ImagingReport> imagingReports,
        CancellationToken cancellationToken)
    {
        try
        {
            var contextParts = new List<string>();

            if (profile.ChronicDiseases.Any())
                contextParts.Add("Chronic Diseases: " + string.Join(", ", profile.ChronicDiseases.Select(d => d.Name)));

            if (profile.Allergies.Any())
                contextParts.Add("Allergies: " + string.Join(", ", profile.Allergies.Select(a => $"{a.Name} ({a.Severity})")));

            if (medicines.Any())
                contextParts.Add("Current Medications: " + string.Join(", ", medicines.Select(m => $"{m.MedicineName} {m.Dosage}")));

            if (labReports.Any())
            {
                var latest = labReports.OrderByDescending(l => l.ReportDate).First();
                contextParts.Add($"Latest Lab Report ({latest.ReportDate}): {latest.LabName}, Dr. {latest.DoctorName}");
            }

            if (imagingReports.Any())
            {
                var latest = imagingReports.OrderByDescending(i => i.ReportDate).First();
                contextParts.Add($"Latest Imaging ({latest.ReportDate}): {latest.ImagingType} — {latest.Impression}");
            }

            if (!contextParts.Any()) return null;

            var context = string.Join("\n", contextParts);
            var aiRequest = new AiChatRequest
            {
                SystemPrompt = ClinicalSummaryPrompt,
                HealthContext = context,
                CurrentUserMessage = $"Generate a clinical summary for patient: {profile.FirstName} {profile.LastName}.",
                MaxOutputTokens = 500
            };

            var response = await aiChatService.GenerateResponseAsync(aiRequest, cancellationToken);
            return response.Content;
        }
        catch
        {
            return null;
        }
    }
}
