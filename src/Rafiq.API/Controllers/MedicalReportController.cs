using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.MedicalReport;
using Rafiq.Application.Features.MedicalReport.Queries.GenerateMedicalReport;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/medical-report")]
public class MedicalReportController(IMediator mediator) : ControllerBase
{
    [HttpGet("{profileId:guid}")]
    public async Task<IActionResult> Generate(
        Guid profileId,
        [FromQuery] ReportType reportType = ReportType.DoctorSummary,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GenerateMedicalReportQuery(profileId, reportType),
            cancellationToken);

        if (!result.Success || result.Data is null)
            return BadRequest(result);

        var reportTypeName = reportType == ReportType.DoctorSummary ? "DoctorSummary" : "CompleteHistory";
        var fileName = $"RafiqMedicalReport_{reportTypeName}_{DateTime.UtcNow:yyyy-MM-dd}.pdf";

        return File(result.Data, "application/pdf", fileName);
    }
}
