using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.LabReports.Commands.DeleteLabReport;

public sealed record DeleteLabReportCommand(Guid Id) : IRequest<ApiResponse<bool>>;