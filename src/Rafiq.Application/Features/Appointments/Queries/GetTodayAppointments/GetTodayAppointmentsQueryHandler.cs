using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Queries.GetTodayAppointments;

public sealed class GetTodayAppointmentsQueryHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetTodayAppointmentsQuery, ApiResponse<List<AppointmentResponseDto>>>
{
    public async Task<ApiResponse<List<AppointmentResponseDto>>> Handle(
        GetTodayAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        _ = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.ProfileId);

        await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var appointments = await appointmentRepository.GetTodayByUserHealthProfileIdAsync(
            request.ProfileId,
            DateTime.UtcNow,
            cancellationToken);

        return ApiResponse<List<AppointmentResponseDto>>.SuccessResponse(
            appointments.Select(x => x.ToDto()).ToList(),
            "Today's appointments retrieved successfully.");
    }
}
