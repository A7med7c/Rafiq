using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Queries.GetAppointments;

public sealed class GetAppointmentsQueryHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetAppointmentsQuery, ApiResponse<List<AppointmentResponseDto>>>
{
    public async Task<ApiResponse<List<AppointmentResponseDto>>> Handle(
        GetAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        _ = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.ProfileId);

        await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var appointments = await appointmentRepository.GetAllByUserHealthProfileIdAsync(request.ProfileId, cancellationToken);

        return ApiResponse<List<AppointmentResponseDto>>.SuccessResponse(
            appointments.Select(x => x.ToDto()).ToList(),
            "Appointments retrieved successfully.");
    }
}
