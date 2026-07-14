using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Commands.DeleteAppointment;

public sealed class DeleteAppointmentCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IAppointmentRepository appointmentRepository,
    IAppointmentReminderScheduler reminderScheduler,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAppointmentCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.Appointment), request.Id);

        await authorizationService.EnsureCanWriteAsync(appointment.UserHealthProfileId, cancellationToken);

        reminderScheduler.CancelJob(appointment.HangfireJobId);

        appointment.SoftDelete();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Appointment deleted successfully.");
    }
}
