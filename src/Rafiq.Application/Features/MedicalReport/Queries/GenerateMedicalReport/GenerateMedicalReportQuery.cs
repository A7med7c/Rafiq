using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.MedicalReport.Queries.GenerateMedicalReport;

public sealed record GenerateMedicalReportQuery(Guid ProfileId, ReportType ReportType)
    : IRequest<ApiResponse<byte[]>>;
