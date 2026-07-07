using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Queries.GetAppointmentById;

public sealed class GetAppointmentByIdQueryHandler(
    ICurrentUserService currentUserService,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetAppointmentByIdQuery, ApiResponse<AppointmentResponseDto>>
{
    public async Task<ApiResponse<AppointmentResponseDto>> Handle(
        GetAppointmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var appointment = await appointmentRepository
            .GetByIdAsync(request.Id, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.Appointment), request.Id);

        return ApiResponse<AppointmentResponseDto>.SuccessResponse(
            appointment.ToDto(),
            "Appointment retrieved successfully.");
    }
}
