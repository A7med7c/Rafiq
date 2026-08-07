using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Common.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Queries.GetUpcomingAppointments;

/// <summary>
/// Returns upcoming appointments mapped to the shared <see cref="UpcomingReminderDto"/> contract.
/// This handler is completely read-only — it does not update missed appointment statuses.
/// Missed-status updates are handled by the <c>UpdateMissedAppointmentsJob</c> Hangfire job.
/// </summary>
public sealed class GetUpcomingAppointmentsQueryHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetUpcomingAppointmentsQuery, ApiResponse<List<UpcomingReminderDto>>>
{
    public async Task<ApiResponse<List<UpcomingReminderDto>>> Handle(
        GetUpcomingAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        _ = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.ProfileId);

        await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var appointments = await appointmentRepository
            .GetUpcomingByUserHealthProfileIdAsync(request.ProfileId, cancellationToken);

        var dtos = appointments.Select(a => new UpcomingReminderDto
        {
            ReminderId  = a.Id,
            EntityId    = a.Id,
            ReminderType = ReminderType.Appointment,
            Title       = a.Title,
            Body        = $"{a.Provider} — {a.AppointmentDateTime:g}",
            ScheduledAt = a.ReminderOffsetMinutes.HasValue
                ? new DateTimeOffset(a.AppointmentDateTime, TimeSpan.Zero)
                      .AddMinutes(-a.ReminderOffsetMinutes.Value)
                : new DateTimeOffset(a.AppointmentDateTime, TimeSpan.Zero),
            Status      = a.Status.ToString(),
            UpdatedAt   = new DateTimeOffset(a.UpdatedAt ?? a.CreatedAt, TimeSpan.Zero),
            IsDeleted   = a.IsDeleted,
            Payload     = null
        }).ToList();

        return ApiResponse<List<UpcomingReminderDto>>.SuccessResponse(
            dtos,
            "Upcoming appointments retrieved successfully.");
    }
}
