using MediatR;
using Microsoft.AspNetCore.Hosting;
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
    IUnitOfWork unitOfWork,
    IWebHostEnvironment webHostEnvironment)
    : IRequestHandler<UploadImagingReportCommand, ApiResponse<ImagingReportResponseDto>>
{
    public async Task<ApiResponse<ImagingReportResponseDto>> Handle(
        UploadImagingReportCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        using var memoryStream = new MemoryStream();
        await request.Image.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);

        var extracted = await bedrockService.AnalyzeAsync<BedrockImagingReportDto>(
            base64Image,
            ImagingReportPrompt.Build(),
            cancellationToken)
            ?? throw new Exception("Bedrock returned no data. Please try again.");

        var reportDate = ParseReportDate(extracted.ReportDate);

        var webRootPath = string.IsNullOrWhiteSpace(webHostEnvironment.WebRootPath)
            ? Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot")
            : webHostEnvironment.WebRootPath;

        var uploadDirectory = Path.Combine(webRootPath, "imaging-reports");
        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(request.Image.FileName)}";
        var physicalPath = Path.Combine(uploadDirectory, fileName);

        await using (var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await request.Image.CopyToAsync(fileStream, cancellationToken);
        }

        var reportImagePath = $"/imaging-reports/{fileName}";

        var imagingReport = new ImagingReport(
            userId: userId,
            reportImagePath: reportImagePath)
        {
            ImagingType = extracted.ImagingType ?? string.Empty,
            BodyPart = extracted.BodyPart ?? string.Empty,
            Findings = extracted.Findings ?? string.Empty,
            Impression = extracted.Impression ?? string.Empty,
            DoctorName = extracted.DoctorName ?? string.Empty,
            ReportDate = reportDate,
            AiSummary = extracted.AiSummary ?? string.Empty
        };

        await imagingReportRepository.AddAsync(imagingReport, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ImagingReportResponseDto>.SuccessResponse(
            ImagingReportMapper.ToDto(imagingReport),
            "Imaging report uploaded and analyzed successfully.");
    }

    private static DateOnly ParseReportDate(string? reportDate)
    {
        if (DateOnly.TryParseExact(
                reportDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return parsed;
        }

        if (DateOnly.TryParse(reportDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return parsed;
        }

        return DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
