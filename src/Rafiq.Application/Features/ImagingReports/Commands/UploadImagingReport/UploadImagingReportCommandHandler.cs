using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;

public sealed class UploadImagingReportCommandHandler(
    ICurrentUserService currentUserService,
    IBedrockService bedrockService,
    IImagingReportRepository imagingReportRepository,
    IFileStorageService fileStorageService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadImagingReportCommand, ApiResponse<ImagingReportResponseDto>>
{
    public async Task<ApiResponse<ImagingReportResponseDto>> Handle(
        UploadImagingReportCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Resolve authenticated user ──────────────────────────────
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        // ── 2. Save physical file via IFileStorageService ─────────────────
        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        using var imageStream = request.Image.OpenReadStream();
        var imageUrl = await fileStorageService.UploadFileAsync(
            imageStream,
            uniqueFileName,
            "imaging",
            cancellationToken);

        // ── 3. Convert to Base64 ONLY for sending to Bedrock ──────────────
        using var memoryStream = new MemoryStream();
        imageStream.Position = 0; // Reset stream pointer to read again
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var base64Image = Convert.ToBase64String(memoryStream.ToArray());

        // ── 4. Analyze with Bedrock ────────────────────────────────────────
        var extracted = await bedrockService.AnalyzeAsync<BedrockImagingReportDto>(
            base64Image,
            ImagingReportPrompt.Build(),
            cancellationToken)
            ?? throw new Exception("Bedrock returned no data. Please try again.");

        // ── 5. Parse report date ──────────────────────────────────────────
        var reportDate = DateOnly.TryParseExact(
            extracted.ReportDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        // ── 6. Build the domain entity (storing only relative path) ────────
        var imagingReport = new ImagingReport(
            userId: userId,
            imagingType: extracted.ImagingType ?? "Unknown",
            bodyPart: extracted.BodyPart ?? "Unknown",
            findings: extracted.Findings ?? "No findings reported.",
            impression: extracted.Impression ?? "No impression reported.",
            doctorName: extracted.DoctorName,
            reportDate: reportDate,
            imageUrl: imageUrl,
            ocrText: extracted.OcrText,
            description: extracted.AiSummary
        );

        // ── 7. Persist inside one transaction ─────────────────────────────
        await imagingReportRepository.AddAsync(imagingReport, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // ── 8. Return response ─────────────────────────────────────────────
        return ApiResponse<ImagingReportResponseDto>.SuccessResponse(
            ToDto(imagingReport),
            "Imaging report uploaded and analyzed successfully.");
    }

    private static ImagingReportResponseDto ToDto(ImagingReport report) =>
        new()
        {
            Id = report.Id,
            ImagingType = report.ImagingType,
            BodyPart = report.BodyPart,
            Findings = report.Findings,
            Impression = report.Impression,
            DoctorName = report.DoctorName,
            ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
            ImageUrl = report.ImageUrl,
            OCRText = report.OCRText,
            Summary = report.Description,
            CreatedAt = report.CreatedAt
        };
}
