using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Commands.UpdateAppointment;

public sealed class UpdateAppointmentCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IAppointmentRepository appointmentRepository,
    IAppointmentReminderScheduler reminderScheduler,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAppointmentCommand, ApiResponse<AppointmentResponseDto>>
{
    public async Task<ApiResponse<AppointmentResponseDto>> Handle(
        UpdateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.Appointment), request.Id);

        await authorizationService.EnsureCanWriteAsync(appointment.UserHealthProfileId, cancellationToken);

        var duplicateExists = await appointmentRepository.ExistsDuplicateAsync(
            appointment.UserHealthProfileId,
            request.AppointmentType,
            request.Title,
            request.Provider,
            request.AppointmentDateTime,
            request.Id,
            cancellationToken);

        if (duplicateExists)
            throw new ValidationException(new[] { "An appointment with the same type, title, provider, and date/time already exists." });

        // Cancel the previous reminder job before updating appointment details.
        reminderScheduler.CancelJob(appointment.HangfireJobId);
        appointment.ClearJobId();

        appointment.UpdateDetails(
            request.AppointmentType,
            request.CustomType,
            request.Title,
            request.Provider,
            request.AppointmentDateTime,
            request.ReminderOffsetMinutes,
            request.Notes);

        var jobId = reminderScheduler.ScheduleReminder(appointment);
        if (jobId is not null)
            appointment.SetJobId(jobId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AppointmentResponseDto>.SuccessResponse(
            appointment.ToDto(),
            "Appointment updated successfully.");
    }
}
