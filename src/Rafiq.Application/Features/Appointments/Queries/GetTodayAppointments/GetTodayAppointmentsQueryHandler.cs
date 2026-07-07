using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Queries.GetTodayAppointments;

public sealed class GetTodayAppointmentsQueryHandler(
    ICurrentUserService currentUserService,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetTodayAppointmentsQuery, ApiResponse<List<AppointmentResponseDto>>>
{
    public async Task<ApiResponse<List<AppointmentResponseDto>>> Handle(
        GetTodayAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var appointments = await appointmentRepository.GetTodayByUserIdAsync(
            userId,
            DateTime.UtcNow,
            cancellationToken);

        return ApiResponse<List<AppointmentResponseDto>>.SuccessResponse(
            appointments.Select(x => x.ToDto()).ToList(),
            "Today's appointments retrieved successfully.");
    }
}
