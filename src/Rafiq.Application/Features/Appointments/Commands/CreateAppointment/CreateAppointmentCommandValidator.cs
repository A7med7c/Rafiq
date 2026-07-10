using FluentValidation;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.Appointments.Commands.CreateAppointment;

internal sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.ProfileId)
            .NotEmpty().WithMessage("ProfileId is required.");

        RuleFor(x => x.AppointmentDateTime)
            .NotEmpty().WithMessage("AppointmentDateTime is required.")
            .Must(x => x > DateTime.UtcNow).WithMessage("AppointmentDateTime cannot be in the past.");

        RuleFor(x => x.AppointmentType)
            .IsInEnum().WithMessage("AppointmentType must be a valid value.");

        RuleFor(x => x.CustomType)
            .NotEmpty().WithMessage("CustomType is required when AppointmentType is Other.")
            .When(x => x.AppointmentType == AppointmentType.Other);

        RuleFor(x => x.CustomType)
            .Must(string.IsNullOrWhiteSpace).WithMessage("CustomType must be null when AppointmentType is not Other.")
            .When(x => x.AppointmentType != AppointmentType.Other);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.");

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required.");

        RuleFor(x => x.ReminderOffsetMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("ReminderOffsetMinutes cannot be negative.")
            .When(x => x.ReminderOffsetMinutes.HasValue);
    }
}
