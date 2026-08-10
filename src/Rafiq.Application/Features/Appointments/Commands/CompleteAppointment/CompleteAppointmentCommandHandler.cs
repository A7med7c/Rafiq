using MediatR;
using Microsoft.Extensions.Logging;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Commands.CompleteAppointment;

public sealed class CompleteAppointmentCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IAppointmentRepository appointmentRepository,
    ILogger<CompleteAppointmentCommandHandler> logger,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CompleteAppointmentCommand, ApiResponse<AppointmentResponseDto>>
{
    public async Task<ApiResponse<AppointmentResponseDto>> Handle(CompleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);

        if (appointment is null) throw new NotFoundException(nameof(Domain.Entities.Documents.Appointment), request.Id);

        await authorizationService.EnsureCanWriteAsync(appointment.UserHealthProfileId, cancellationToken);

        var appointmentUtcTime = DateTime.SpecifyKind(appointment.AppointmentDateTime, DateTimeKind.Utc);

        if (dateTimeProvider.UtcNow >= appointmentUtcTime.AddHours(24))
        {
            if (appointment.Status != AppointmentStatus.Missed)
            {
                appointment.MarkAsMissed();
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            throw new BadRequestException("Appointment cannot be confirmed because 24 hours have passed since its scheduled time.");
        }

        appointment.MarkAsCompleted();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AppointmentResponseDto>.SuccessResponse(appointment.ToDto(), "Appointment completed successfully.");
    }
}
