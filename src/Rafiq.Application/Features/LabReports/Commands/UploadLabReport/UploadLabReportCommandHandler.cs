using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.LabReports.Commands.UploadLabReport;

public sealed class UploadLabReportCommandHandler(
    ICurrentUserService currentUserService,
    IBedrockService bedrockService,
    ILabReportRepository labReportRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadLabReportCommand, ApiResponse<LabReportResponseDto>>
{
    public async Task<ApiResponse<LabReportResponseDto>> Handle(
        UploadLabReportCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Resolve the authenticated user ──────────────────────────────
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        // ── 2. Convert the uploaded image to Base64 ────────────────────────
        using var memoryStream = new MemoryStream();
        await request.Image.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);

        // ── 3. Analyze with Bedrock ────────────────────────────────────────
        var extracted = await bedrockService.AnalyzeAsync<BedrockLabReportDto>(
            base64Image,
            LabReportPrompt.Build(),
            cancellationToken)
            ?? throw new Exception("Bedrock returned no data. Please try again.");

        if (extracted.Tests.Count == 0)
            throw new Exception("No laboratory tests could be extracted from the uploaded image.");

        // ── 4. Resolve the DocumentType (lazy-create if first time) ────────
        var documentTypeId = await labReportRepository
            .GetOrCreateDocumentTypeIdAsync("Lab Report", cancellationToken);

        // ── 5. Parse the report date ───────────────────────────────────────
        var reportDate = DateOnly.TryParseExact(
            extracted.ReportDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var title = $"Lab Report — {extracted.LabName ?? "Unknown Lab"} — {reportDate:yyyy-MM-dd}";

        // ── 6. Build the domain entity ─────────────────────────────────────
        var labReport = new LabReport(
            userId: userId,
            documentTypeId: documentTypeId,
            title: title,
            imageData: imageBytes,
            description: extracted.Summary,
            ocrText: extracted.OcrText)
        {
            LabName = extracted.LabName ?? string.Empty,
            DoctorName = extracted.DoctorName ?? string.Empty,
            ReportDate = reportDate
        };

        // ── 7. Map every extracted test into LabResult entities ────────────
        foreach (var test in extracted.Tests)
        {
            labReport.Results.Add(new LabResult
            {
                TestName = test.TestName ?? string.Empty,
                Value = test.Value ?? string.Empty,
                Unit = test.Unit ?? string.Empty,
                NormalRange = test.NormalRange ?? string.Empty,
                Status = test.Status
            });
        }

        // ── 8. Persist inside one transaction (EF tracks the whole graph) ──
        await labReportRepository.AddAsync(labReport, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // ── 9. Return the response ─────────────────────────────────────────
        return ApiResponse<LabReportResponseDto>.SuccessResponse(
            ToDto(labReport),
            "Lab report uploaded and analyzed successfully.");
    }
    // ── Mapping helper ─────────────────────────────────────────────────────
    private static LabReportResponseDto ToDto(LabReport report) =>
        new()
        {
            Id = report.Id,
            LabName = report.LabName,
            DoctorName = report.DoctorName,
            ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
            //ImageUrl = report.ImageUrl,
            OCRText = report.OCRText,
            Summary = report.Description,
            CreatedAt = report.CreatedAt,
            Results = report.Results.Select(r => new LabResultResponseDto
            {
                Id = r.Id,
                TestName = r.TestName,
                Value = r.Value,
                Unit = r.Unit,
                NormalRange = r.NormalRange,
                Status = r.Status
            }).ToList()
        };
}
