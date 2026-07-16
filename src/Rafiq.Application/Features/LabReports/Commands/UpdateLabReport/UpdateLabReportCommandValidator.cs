using FluentValidation;
using System.Globalization;

namespace Rafiq.Application.Features.LabReports.Commands.UpdateLabReport;

internal sealed class UpdateLabReportCommandValidator : AbstractValidator<UpdateLabReportCommand>
{
    public UpdateLabReportCommandValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.ReportDate), () =>
        {
            RuleFor(x => x.ReportDate)
                .Must(NotBeInFuture)
                .WithMessage("Date cannot be later than today.");
        });
    }

    private static bool NotBeInFuture(string? date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return true;
        return parsed <= DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
