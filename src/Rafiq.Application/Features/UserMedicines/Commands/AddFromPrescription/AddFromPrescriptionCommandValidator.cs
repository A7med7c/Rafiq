using FluentValidation;

namespace Rafiq.Application.Features.UserMedicines.Commands.AddFromPrescription;

public sealed class AddFromPrescriptionCommandValidator : AbstractValidator<AddFromPrescriptionCommand>
{
    public AddFromPrescriptionCommandValidator()
    {
        RuleFor(x => x.PrescriptionMedicineIds)
            .NotEmpty().WithMessage("Validation.AtLeastOneMedicineIDMustBeProv");
            
        RuleForEach(x => x.PrescriptionMedicineIds)
            .NotEmpty().WithMessage("Validation.MedicineIDCannotBeEmpty");
    }
}
