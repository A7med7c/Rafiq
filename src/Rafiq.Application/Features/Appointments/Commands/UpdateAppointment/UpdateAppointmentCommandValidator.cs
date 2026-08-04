using FluentValidation;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.Appointments.Commands.UpdateAppointment;

internal sealed class UpdateAppointmentCommandValidator : AbstractValidator<UpdateAppointmentCommand>
{
    public UpdateAppointmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Validation.AppointmentIdIsRequired");

        RuleFor(x => x.AppointmentDateTime)
            .NotEmpty().WithMessage("Validation.AppointmentDateTimeIsRequired")
            .Must(x => x > DateTime.UtcNow).WithMessage("Validation.AppointmentDateTimeCannotBeInT");

        RuleFor(x => x.AppointmentType)
            .IsInEnum().WithMessage("Validation.AppointmentTypeMustBeAValidVal");

        RuleFor(x => x.CustomType)
            .NotEmpty().WithMessage("Validation.CustomTypeIsRequiredWhenAppoin")
            .When(x => x.AppointmentType == AppointmentType.Other);

        RuleFor(x => x.CustomType)
            .Must(string.IsNullOrWhiteSpace).WithMessage("Validation.CustomTypeMustBeNullWhenAppoin")
            .When(x => x.AppointmentType != AppointmentType.Other);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Validation.TitleIsRequired");

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Validation.ProviderIsRequired");

        RuleFor(x => x.ReminderOffsetMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Validation.ReminderOffsetMinutesCannotBeN")
            .When(x => x.ReminderOffsetMinutes.HasValue);
    }
}
