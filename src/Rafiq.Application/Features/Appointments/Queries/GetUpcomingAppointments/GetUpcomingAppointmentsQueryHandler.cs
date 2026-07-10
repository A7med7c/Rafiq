using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Queries.GetUpcomingAppointments;

public sealed class GetUpcomingAppointmentsQueryHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetUpcomingAppointmentsQuery, ApiResponse<List<AppointmentResponseDto>>>
{
    public async Task<ApiResponse<List<AppointmentResponseDto>>> Handle(
        GetUpcomingAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        _ = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.ProfileId);

        await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var appointments = await appointmentRepository.GetUpcomingByUserHealthProfileIdAsync(request.ProfileId, cancellationToken);

        return ApiResponse<List<AppointmentResponseDto>>.SuccessResponse(
            appointments.Select(x => x.ToDto()).ToList(),
            "Upcoming appointments retrieved successfully.");
    }
}
