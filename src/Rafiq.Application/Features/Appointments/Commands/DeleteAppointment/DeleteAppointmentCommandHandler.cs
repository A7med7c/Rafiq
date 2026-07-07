using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Commands.DeleteAppointment;

public sealed class DeleteAppointmentCommandHandler(
    ICurrentUserService currentUserService,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAppointmentCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var deleted = await appointmentRepository.DeleteAsync(request.Id, userId, cancellationToken);

        if (!deleted)
            throw new NotFoundException(nameof(Domain.Entities.Documents.Appointment), request.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Appointment deleted successfully.");
    }
}
