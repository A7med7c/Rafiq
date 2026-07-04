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
    IFileStorageService fileStorageService,
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

        // ── 2. Save physical file via IFileStorageService ─────────────────
        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        using var imageStream = request.Image.OpenReadStream();
        var imageUrl = await fileStorageService.UploadFileAsync(
            imageStream,
            uniqueFileName,
            "labs",
            cancellationToken);

        // ── 3. Convert to Base64 ONLY for sending to Bedrock ──────────────
        // We still need the base64 string because the multimodal API needs it,
        // but we DO NOT save it in the database.
        using var memoryStream = new MemoryStream();
        imageStream.Position = 0; // Reset position to read again
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var base64Image = Convert.ToBase64String(memoryStream.ToArray());

        // ── 4. Analyze with Bedrock ────────────────────────────────────────
        var extracted = await bedrockService.AnalyzeAsync<BedrockLabReportDto>(
            base64Image,
            LabReportPrompt.Build(),
            cancellationToken)
            ?? throw new Exception("Bedrock returned no data. Please try again.");

        if (extracted.Tests.Count == 0)
            throw new Exception("No laboratory tests could be extracted from the uploaded image.");

        // ── 5. Parse the report date ───────────────────────────────────────
        var reportDate = DateOnly.TryParseExact(
            extracted.ReportDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        // ── 6. Build the domain entity (storing only relative path) ────────
        var labReport = new LabReport(
            userId: userId,
            doctorName: extracted.DoctorName ?? string.Empty,
            labName: extracted.LabName ?? string.Empty,
            reportDate: reportDate,
            imageUrl: imageUrl,
            ocrText: extracted.OcrText,
            description: extracted.Summary
        );

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
            OCRText = report.OCRText,
            Summary = report.Description,
            ImageUrl = report.ImageUrl,
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
