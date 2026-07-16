using FluentValidation;
using System.Globalization;

namespace Rafiq.Application.Features.Prescriptions.Commands.UpdatePrescription;

internal sealed class UpdatePrescriptionCommandValidator
    : AbstractValidator<UpdatePrescriptionCommand>
{
    public UpdatePrescriptionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Prescription Id is required.");

        RuleFor(x => x.DoctorName)
            .NotEmpty()
            .WithMessage("Doctor name is required.")
            .MaximumLength(150)
            .WithMessage("Doctor name must not exceed 150 characters.");

        RuleFor(x => x.PatientName)
            .NotEmpty()
            .WithMessage("Patient name is required.")
            .MaximumLength(200)
            .WithMessage("Patient name must not exceed 200 characters.");

        RuleFor(x => x.PrescriptionDate)
            .NotEmpty()
            .WithMessage("Prescription date is required.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$")
            .WithMessage("Prescription date must be in yyyy-MM-dd format.")
            .Must(NotBeInFuture)
            .WithMessage("Date cannot be later than today.");
    }

    private static bool NotBeInFuture(string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return true;
        return parsed <= DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
