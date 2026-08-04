using FluentValidation;
using System.Globalization;

namespace Rafiq.Application.Features.ImagingReports.Commands.SaveImagingReport;

internal sealed class SaveImagingReportCommandValidator : AbstractValidator<SaveImagingReportCommand>
{
    public SaveImagingReportCommandValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.ReportDate), () =>
        {
            RuleFor(x => x.ReportDate)
                .Must(NotBeInFuture)
                .WithMessage("Validation.DateCannotBeLaterThanToday");
        });
    }

    private static bool NotBeInFuture(string? date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return true;
        return parsed <= DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
