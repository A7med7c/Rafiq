using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.LabReports.Commands.UploadLabReport;

public sealed class UploadLabReportCommandHandler(
    ICurrentUserService currentUserService,
    IBedrockService bedrockService,
    IFileStorageService fileStorageService)
    : IRequestHandler<UploadLabReportCommand, ApiResponse<LabReportResponseDto>>
{
    public async Task<ApiResponse<LabReportResponseDto>> Handle(
        UploadLabReportCommand request,
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
            "labs",
            cancellationToken);

        using var memoryStream = new MemoryStream();
        imageStream.Position = 0;
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var base64Image = Convert.ToBase64String(memoryStream.ToArray());

        var extracted = await bedrockService.AnalyzeAsync<BedrockLabReportDto>(
            base64Image,
            LabReportPrompt.Build(),
            cancellationToken)
            ?? throw new BadRequestException("No lab report data could be extracted from the uploaded image.");

        if (extracted.Tests.Count == 0)
            throw new BadRequestException("No laboratory tests could be extracted from the uploaded image.");

        var reportDate = DateOnly.TryParseExact(
            extracted.ReportDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var preview = new LabReportResponseDto
        {
            Id = Guid.Empty,
            LabName = extracted.LabName ?? string.Empty,
            DoctorName = extracted.DoctorName ?? string.Empty,
            ReportDate = reportDate.ToString("yyyy-MM-dd"),
            OCRText = extracted.OcrText,
            Summary = extracted.Summary,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
            Results = extracted.Tests.Select(test => new LabResultResponseDto
            {
                Id = Guid.NewGuid(),
                TestName = test.TestName ?? string.Empty,
                Value = test.Value ?? string.Empty,
                Unit = test.Unit ?? string.Empty,
                NormalRange = test.NormalRange ?? string.Empty,
                Status = test.Status
            }).ToList()
        };

        return ApiResponse<LabReportResponseDto>.SuccessResponse(
            preview,
            "Lab report analyzed successfully. Review before saving.");
    }
}
