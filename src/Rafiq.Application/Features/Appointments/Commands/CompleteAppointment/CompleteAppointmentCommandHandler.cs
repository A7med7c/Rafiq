using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Commands.CompleteAppointment;

public sealed class CompleteAppointmentCommandHandler(
    ICurrentUserService currentUserService,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteAppointmentCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(CompleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var appointment = await appointmentRepository
            .GetByIdAsync(request.Id, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.Appointment), request.Id);

        appointment.MarkAsCompleted();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Appointment completed successfully.");
    }
}
