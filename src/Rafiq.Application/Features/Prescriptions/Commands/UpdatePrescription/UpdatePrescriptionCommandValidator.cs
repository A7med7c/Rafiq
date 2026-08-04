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
            .WithMessage("Validation.PrescriptionIdIsRequired");

        RuleFor(x => x.DoctorName)
            .NotEmpty()
            .WithMessage("Validation.DoctorNameIsRequired")
            .MaximumLength(150)
            .WithMessage("Validation.DoctorNameMustNotExceed150Char");

        RuleFor(x => x.PatientName)
            .NotEmpty()
            .WithMessage("Validation.PatientNameIsRequired")
            .MaximumLength(200)
            .WithMessage("Validation.PatientNameMustNotExceed200Cha");

        RuleFor(x => x.PrescriptionDate)
            .NotEmpty()
            .WithMessage("Validation.PrescriptionDateIsRequired")
            .Matches(@"^\d{4}-\d{2}-\d{2}$")
            .WithMessage("Validation.PrescriptionDateMustBeInYyyyMM")
            .Must(NotBeInFuture)
            .WithMessage("Validation.DateCannotBeLaterThanToday");
    }

    private static bool NotBeInFuture(string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return true;
        return parsed <= DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
