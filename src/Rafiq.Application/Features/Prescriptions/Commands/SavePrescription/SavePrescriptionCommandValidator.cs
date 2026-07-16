using FluentValidation;
using System.Globalization;

namespace Rafiq.Application.Features.Prescriptions.Commands.SavePrescription;

internal sealed class SavePrescriptionCommandValidator : AbstractValidator<SavePrescriptionCommand>
{
    public SavePrescriptionCommandValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.PrescriptionDate), () =>
        {
            RuleFor(x => x.PrescriptionDate)
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
