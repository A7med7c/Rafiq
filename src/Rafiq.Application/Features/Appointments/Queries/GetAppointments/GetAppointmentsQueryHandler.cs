using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Queries.GetAppointments;

public sealed class GetAppointmentsQueryHandler(
    ICurrentUserService currentUserService,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetAppointmentsQuery, ApiResponse<List<AppointmentResponseDto>>>
{
    public async Task<ApiResponse<List<AppointmentResponseDto>>> Handle(
        GetAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var appointments = await appointmentRepository.GetAllByUserIdAsync(userId, cancellationToken);

        return ApiResponse<List<AppointmentResponseDto>>.SuccessResponse(
            appointments.Select(x => x.ToDto()).ToList(),
            "Appointments retrieved successfully.");
    }
}
