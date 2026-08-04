using FluentValidation;

namespace Rafiq.Application.Features.UserMedicines.Commands.AddUserMedicine;

internal sealed class AddUserMedicineCommandValidator
    : AbstractValidator<AddUserMedicineCommand>
{
    public AddUserMedicineCommandValidator()
    {
        RuleFor(x => x.MedicineName)
            .NotEmpty().WithMessage("Validation.MedicineNameIsRequired")
            .MaximumLength(300).WithMessage("Validation.MedicineNameMustNotExceed300Ch");

        // Dosage is intentionally optional: many users don't know the exact dosage.
        // A blank string is stored and shown as "Not specified" in the UI.
        RuleFor(x => x.Dosage)
            .MaximumLength(200).WithMessage("Validation.DosageMustNotExceed200Characte");

        RuleFor(x => x.Frequency)
            .NotEmpty().WithMessage("Validation.FrequencyIsRequired")
            .MaximumLength(200).WithMessage("Validation.FrequencyMustNotExceed200Chara");

        RuleFor(x => x.Duration)
            .NotEmpty().WithMessage("Validation.DurationIsRequired")
            .MaximumLength(200).WithMessage("Validation.DurationMustNotExceed200Charac");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Validation.InvalidSourceType");
    }
}
