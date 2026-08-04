using FluentValidation;

namespace Rafiq.Application.Features.UserMedicines.Commands.UpdateUserMedicine;

internal sealed class UpdateUserMedicineCommandValidator
    : AbstractValidator<UpdateUserMedicineCommand>
{
    public UpdateUserMedicineCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Validation.MedicineIdIsRequired");

        RuleFor(x => x.MedicineName)
            .NotEmpty().WithMessage("Validation.MedicineNameIsRequired")
            .MaximumLength(300).WithMessage("Validation.MedicineNameMustNotExceed300Ch");

        // Dosage is intentionally optional (matches AddUserMedicine): many users
        // don't know the exact dosage. Empty string is stored and shown as "Not specified".
        RuleFor(x => x.Dosage)
            .MaximumLength(200).WithMessage("Validation.DosageMustNotExceed200Characte");

        RuleFor(x => x.Frequency)
            .NotEmpty().WithMessage("Validation.FrequencyIsRequired")
            .MaximumLength(200).WithMessage("Validation.FrequencyMustNotExceed200Chara");

        RuleFor(x => x.Duration)
            .NotEmpty().WithMessage("Validation.DurationIsRequired")
            .MaximumLength(200).WithMessage("Validation.DurationMustNotExceed200Charac");
    }
}
