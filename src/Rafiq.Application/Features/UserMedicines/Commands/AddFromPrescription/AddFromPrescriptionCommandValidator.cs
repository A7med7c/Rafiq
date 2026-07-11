using FluentValidation;

namespace Rafiq.Application.Features.UserMedicines.Commands.AddFromPrescription;

public sealed class AddFromPrescriptionCommandValidator : AbstractValidator<AddFromPrescriptionCommand>
{
    public AddFromPrescriptionCommandValidator()
    {
        RuleFor(x => x.PrescriptionMedicineIds)
            .NotEmpty().WithMessage("At least one medicine ID must be provided.");
            
        RuleForEach(x => x.PrescriptionMedicineIds)
            .NotEmpty().WithMessage("Medicine ID cannot be empty.");
    }
}
