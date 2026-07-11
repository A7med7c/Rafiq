using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.ImagingReports.Commands.DeleteImagingReport;

public sealed record DeleteImagingReportCommand(Guid Id) : IRequest<ApiResponse<bool>>;