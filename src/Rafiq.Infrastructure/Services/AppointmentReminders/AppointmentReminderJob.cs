using Hangfire;
using Microsoft.Extensions.Logging;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Services.AppointmentReminders;

public sealed class AppointmentReminderJob(
    IAppointmentRepository appointmentRepository,
    INotificationService notificationService,
    ILogger<AppointmentReminderJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(Guid appointmentId)
    {
        logger.LogInformation("AppointmentReminderJob STARTED for appointment {AppointmentId}.", appointmentId);

        var appointment = await appointmentRepository.GetByIdWithDetailsAsync(appointmentId, CancellationToken.None);

        if (appointment is null)
        {
            logger.LogWarning("Appointment {AppointmentId} not found. Skipping.", appointmentId);
            return;
        }

        if (appointment.IsDeleted)
        {
            logger.LogInformation("Appointment {AppointmentId} has been deleted. Skipping reminder.", appointmentId);
            return;
        }

        if (appointment.Status != AppointmentStatus.Upcoming)
        {
            logger.LogInformation(
                "Appointment {AppointmentId} is {Status}. Skipping reminder.",
                appointmentId, appointment.Status);
            return;
        }

        var profile = appointment.UserHealthProfile;
        if (profile?.UserId is null)
        {
            logger.LogWarning(
                "Profile {ProfileId} has no associated user. Skipping appointment reminder notification.",
                appointment.UserHealthProfileId);
            return;
        }

        var appointmentTime = appointment.AppointmentDateTime.ToString("h:mm tt");
        var notificationText = $"You have an appointment with {appointment.Provider} at {appointmentTime}.";

        var payload = new AppointmentReminderNotificationPayload
        {
            AppointmentId = appointment.Id.ToString(),
            Title = appointment.Title,
            Provider = appointment.Provider,
            AppointmentDateTime = appointment.AppointmentDateTime.ToString("o"),
            NotificationText = notificationText,
            AppointmentType = appointment.AppointmentType.ToString(),
            CustomType = appointment.CustomType,
        };

        await notificationService.SendAppointmentReminderAsync(
            profile.UserId.ToString()!,
            payload,
            CancellationToken.None);

        logger.LogInformation(
            "Sent appointment reminder for {AppointmentId} to user {UserId}.",
            appointmentId, profile.UserId);
    }
}
