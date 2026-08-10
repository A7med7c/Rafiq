using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence.Identity;
using Rafiq.Infrastructure.Services.Notifications;

namespace Rafiq.Infrastructure.Services.AppointmentReminders;

public sealed class AppointmentReminderJob(
    IAppointmentRepository appointmentRepository,
    INotificationService notificationService,
    IWhatsAppService whatsAppService,
    IEmergencyContactRepository emergencyContactRepository,
    UserManager<ApplicationUser> userManager,
    IDateTimeProvider dateTimeProvider,
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

        var utcTime = DateTime.SpecifyKind(appointment.AppointmentDateTime, DateTimeKind.Utc);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, dateTimeProvider.ReminderTimeZone);
        var appointmentTimeStr = localTime.ToString("h:mm tt");
        var notificationText = $"You have an appointment with {appointment.Provider} at {appointmentTimeStr}.";

        var payload = new AppointmentReminderNotificationPayload
        {
            NotificationId = Guid.NewGuid().ToString(),
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

        await SendWhatsAppRemindersSafeAsync(appointment, profile.UserId.Value);
    }

    private async Task SendWhatsAppRemindersSafeAsync(Appointment appointment, Guid userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (string.IsNullOrEmpty(user?.PhoneNumber))
            {
                logger.LogWarning("No phone number found for user {UserId}. Skipping WhatsApp appointment reminders.", userId);
                return;
            }

            var patientName = $"{appointment.UserHealthProfile.FirstName} {appointment.UserHealthProfile.LastName}";
            var utcTime = DateTime.SpecifyKind(appointment.AppointmentDateTime, DateTimeKind.Utc);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, dateTimeProvider.ReminderTimeZone);
            var appointmentDate = localTime.ToString("dd/MM/yyyy");
            var appointmentTime = localTime.ToString("h:mm tt");
            var templateName = whatsAppOptions.Value.AppointmentReminderTemplate;
            var familyTemplateName = whatsAppOptions.Value.FamilyAppointmentReminderTemplate;

            // Send to patient
            await SendSafeAsync(
                user.PhoneNumber, 
                templateName, 
                [patientName, appointment.Provider, appointmentDate, appointmentTime], 
                "patient");

            // Send to emergency contacts
            var emergencyContacts = await emergencyContactRepository.GetAllByUserIdAsync(userId, CancellationToken.None);
            foreach (var contact in emergencyContacts)
            {
                await SendSafeAsync(
                    contact.PhoneNumber, 
                    familyTemplateName, 
                    [contact.Name, patientName, appointment.Provider, appointmentDate, appointmentTime], 
                    $"emergency contact '{contact.Name}'");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process WhatsApp appointment reminders for appointment {AppointmentId}.", appointment.Id);
        }
    }

    private async Task SendSafeAsync(string phoneNumber, string templateName, List<string> parameters, string recipientLabel)
    {
        try
        {
            await whatsAppService.SendTemplateAsync(phoneNumber, templateName, parameters, CancellationToken.None);
            logger.LogInformation("Successfully sent WhatsApp appointment reminder to {RecipientLabel}.", recipientLabel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WhatsApp send failed for {RecipientLabel} ({PhoneNumber}).", recipientLabel, phoneNumber);
        }
    }
}
