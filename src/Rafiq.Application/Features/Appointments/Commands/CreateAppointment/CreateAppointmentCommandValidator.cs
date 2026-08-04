using FluentValidation;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.Appointments.Commands.CreateAppointment;

internal sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.ProfileId)
            .NotEmpty().WithMessage("Validation.ProfileIdIsRequired");

        RuleFor(x => x.AppointmentDateTime)
            .NotEmpty().WithMessage("Validation.AppointmentDateTimeIsRequired")
            // Normalise to UTC before comparing so Local/Utc/Unspecified kinds are all handled correctly.
            .Must(x => x.ToUniversalTime() > DateTime.UtcNow).WithMessage("Validation.AppointmentDateTimeCannotBeInT");

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
