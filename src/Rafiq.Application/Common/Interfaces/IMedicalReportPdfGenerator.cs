using Rafiq.Application.Features.MedicalReport.DTOs;

namespace Rafiq.Application.Common.Interfaces;

public interface IMedicalReportPdfGenerator
{
    byte[] Generate(MedicalReportDataDto data);
}
