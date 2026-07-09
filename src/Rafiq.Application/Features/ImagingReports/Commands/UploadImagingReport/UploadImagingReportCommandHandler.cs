using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Exceptions;
using System.Globalization;

namespace Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;

public sealed class UploadImagingReportCommandHandler(
    ICurrentUserService currentUserService,
    IBedrockService bedrockService,
    IFileStorageService fileStorageService)
    : IRequestHandler<UploadImagingReportCommand, ApiResponse<ImagingReportResponseDto>>
{
    public async Task<ApiResponse<ImagingReportResponseDto>> Handle(
        UploadImagingReportCommand request,
        CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        using var imageStream = request.Image.OpenReadStream();
        var imageUrl = await fileStorageService.UploadFileAsync(
            imageStream,
            uniqueFileName,
            "imaging",
            cancellationToken);

        using var memoryStream = new MemoryStream();
        imageStream.Position = 0;
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var base64Image = Convert.ToBase64String(memoryStream.ToArray());

        var extracted = await bedrockService.AnalyzeAsync<BedrockImagingReportDto>(
            base64Image,
            ImagingReportPrompt.Build(),
            cancellationToken)
            ?? throw new Exception("Bedrock returned no data. Please try again.");

        var reportDate = DateOnly.TryParseExact(
            extracted.ReportDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var preview = new ImagingReportResponseDto
        {
            Id = Guid.Empty,
            ImagingType = extracted.ImagingType ?? "Unknown",
            BodyPart = extracted.BodyPart ?? "Unknown",
            Findings = extracted.Findings ?? "No findings reported.",
            Impression = extracted.Impression ?? "No impression reported.",
            DoctorName = extracted.DoctorName,
            ReportDate = reportDate.ToString("yyyy-MM-dd"),
            ImageUrl = imageUrl,
            OCRText = extracted.OcrText,
            Summary = extracted.AiSummary,
            CreatedAt = DateTime.UtcNow
        };

        return ApiResponse<ImagingReportResponseDto>.SuccessResponse(
            preview,
            "Imaging report analyzed successfully. Review before saving.");
    }
}
