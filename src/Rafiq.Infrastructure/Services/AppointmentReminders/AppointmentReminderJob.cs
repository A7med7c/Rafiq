using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence.Identity;
using Rafiq.Infrastructure.Services.Notifications;

namespace Rafiq.Infrastructure.Services.AppointmentReminders;

public sealed class AppointmentReminderJob(
    IAppointmentRepository appointmentRepository,
    INotificationService notificationService,
    IWhatsAppService whatsAppService,
    UserManager<ApplicationUser> userManager,
    IOptions<WhatsAppSettings> whatsAppOptions,
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

        await SendWhatsAppAppointmentReminderAsync(appointmentId, profile.UserId.Value, profile.FirstName, profile.LastName, appointment.Provider, appointment.AppointmentDateTime);
    }

    private async Task SendWhatsAppAppointmentReminderAsync(
        Guid appointmentId,
        Guid userId,
        string firstName,
        string lastName,
        string provider,
        DateTime appointmentDateTime)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user?.PhoneNumber is null)
        {
            logger.LogWarning(
                "No phone number found for user {UserId}. Skipping WhatsApp reminder for appointment {AppointmentId}.",
                userId, appointmentId);
            return;
        }

        var patientName = $"{firstName} {lastName}";
        var appointmentDate = appointmentDateTime.ToString("dd/MM/yyyy");
        var appointmentTime = appointmentDateTime.ToString("h:mm tt");
        var templateName = whatsAppOptions.Value.AppointmentReminderTemplate;

        logger.LogInformation(
            "Sending WhatsApp template '{Template}' to '{Phone}' for appointment {AppointmentId}.",
            templateName, user.PhoneNumber, appointmentId);

        try
        {
            await whatsAppService.SendTemplateAsync(
                user.PhoneNumber,
                templateName,
                [patientName, provider, appointmentDate, appointmentTime],
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            // WhatsApp failure must not abort the job — the SignalR notification already fired.
            logger.LogError(ex,
                "Failed to send WhatsApp reminder for appointment {AppointmentId} to user {UserId}.",
                appointmentId, userId);
        }
    }
}
