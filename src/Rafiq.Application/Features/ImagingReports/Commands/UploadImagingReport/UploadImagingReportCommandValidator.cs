using FluentValidation;

namespace Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;

internal sealed class UploadImagingReportCommandValidator
    : AbstractValidator<UploadImagingReportCommand>
{
    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".bmp"];

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadImagingReportCommandValidator()
    {
        RuleFor(x => x.Image)
            .NotNull()
            .WithMessage("An image file is required.");

        RuleFor(x => x.Image.Length)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"The file must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.");

        RuleFor(x => x.Image.FileName)
            .Must(HaveAllowedExtension)
            .WithMessage($"Only the following file types are allowed: {string.Join(", ", AllowedExtensions)}.");
    }

    private static bool HaveAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}
