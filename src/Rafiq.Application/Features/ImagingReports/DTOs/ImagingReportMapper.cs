using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Application.Features.ImagingReports.DTOs;

internal static class ImagingReportMapper
{
    public static ImagingReportResponseDto ToDto(ImagingReport report) =>
        new()
        {
            ReportId = report.ReportId,
            ImagingType = report.ImagingType,
            BodyPart = report.BodyPart,
            Findings = report.Findings,
            Impression = report.Impression,
            DoctorName = report.DoctorName,
            ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
            AiSummary = report.AiSummary,
            UserId = report.UserId,
            ReportImagePath = report.ReportImagePath
        };
}
