using FluentValidation;

namespace Rafiq.Application.Features.UserMedicines.Commands.UpdateUserMedicine;

internal sealed class UpdateUserMedicineCommandValidator
    : AbstractValidator<UpdateUserMedicineCommand>
{
    public UpdateUserMedicineCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Medicine Id is required.");

        RuleFor(x => x.MedicineName)
            .NotEmpty().WithMessage("Medicine name is required.")
            .MaximumLength(300).WithMessage("Medicine name must not exceed 300 characters.");

        RuleFor(x => x.Dosage)
            .NotEmpty().WithMessage("Dosage is required.")
            .MaximumLength(200).WithMessage("Dosage must not exceed 200 characters.");

        RuleFor(x => x.Frequency)
            .NotEmpty().WithMessage("Frequency is required.")
            .MaximumLength(200).WithMessage("Frequency must not exceed 200 characters.");

        RuleFor(x => x.Duration)
            .NotEmpty().WithMessage("Duration is required.")
            .MaximumLength(200).WithMessage("Duration must not exceed 200 characters.");
    }
}
