using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Commands.SnoozeAppointmentReminder;

public sealed class SnoozeAppointmentReminderCommandHandler(
    IAppointmentRepository appointmentRepository,
    IAppointmentReminderScheduler appointmentReminderScheduler,
    IHealthProfileAuthorizationService authorizationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SnoozeAppointmentReminderCommand, ApiResponseBase>
{
    private const int MinSnoozeMinutes = 1;
    private const int MaxSnoozeMinutes = 120;

    public async Task<ApiResponseBase> Handle(
        SnoozeAppointmentReminderCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SnoozeMinutes < MinSnoozeMinutes || request.SnoozeMinutes > MaxSnoozeMinutes)
            throw new BadRequestException(
                $"Snooze interval must be between {MinSnoozeMinutes} and {MaxSnoozeMinutes} minutes.");

        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
            throw new NotFoundException(nameof(Domain.Entities.Documents.Appointment), request.AppointmentId);

        await authorizationService.EnsureCanWriteAsync(appointment.UserHealthProfileId, cancellationToken);

        if (appointment.Status != AppointmentStatus.Upcoming)
            throw new BadRequestException("Only upcoming appointments can be snoozed.");

        appointmentReminderScheduler.CancelJob(appointment.HangfireJobId);
        
        var newJobId = appointmentReminderScheduler.ScheduleSnoozedReminder(appointment, request.SnoozeMinutes);
        if (newJobId is not null)
        {
            appointment.SetJobId(newJobId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponseBase.SuccessResponse($"Appointment reminder snoozed for {request.SnoozeMinutes} minutes.");
    }
}
