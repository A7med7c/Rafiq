using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Queries.GetAppointmentById;

public sealed class GetAppointmentByIdQueryHandler(
    IHealthProfileAuthorizationService authorizationService,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetAppointmentByIdQuery, ApiResponse<AppointmentResponseDto>>
{
    public async Task<ApiResponse<AppointmentResponseDto>> Handle(
        GetAppointmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.Appointment), request.Id);

        await authorizationService.EnsureCanReadAsync(appointment.UserHealthProfileId, cancellationToken);

        return ApiResponse<AppointmentResponseDto>.SuccessResponse(
            appointment.ToDto(),
            "Appointment retrieved successfully.");
    }
}
