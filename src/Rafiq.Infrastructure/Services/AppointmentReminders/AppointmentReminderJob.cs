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
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IWhatsAppService whatsAppService,
    UserManager<ApplicationUser> userManager,
    IOptions<WhatsAppSettings> whatsAppOptions,
    ILogger<AppointmentReminderJob> logger)
{
    // Managed Profiles have no owning user, so reminders route to the members responsible for
    // managing them. Viewer-only members are deliberately excluded from appointment reminders.
    private static readonly AccessRole[] ManagementRoles = [AccessRole.Owner, AccessRole.Manager];

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
        if (profile is null)
        {
            logger.LogWarning(
                "Appointment {AppointmentId} has no health profile loaded. Skipping reminder notification.",
                appointmentId);
            return;
        }

        // Case 1: Self Profile — deliver to the owning user. Case 2: Managed Profile (UserId == null)
        // — deliver to every active Owner/Manager. Distinct ids guarantee one notification per user.
        var recipientUserIds = profile.UserId is not null
            ? [profile.UserId.Value]
            : await healthProfileAccessRepository.GetActiveGranteeUserIdsByRolesAsync(
                profile.Id, ManagementRoles, CancellationToken.None);

        if (recipientUserIds.Count == 0)
        {
            logger.LogInformation(
                "Profile {ProfileId} has no active Owner/Manager recipient. Skipping appointment reminder.",
                profile.Id);
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

        foreach (var recipientUserId in recipientUserIds)
        {
            await notificationService.SendAppointmentReminderAsync(
                recipientUserId.ToString(),
                payload,
                CancellationToken.None);

            logger.LogInformation(
                "Sent appointment reminder for {AppointmentId} to user {UserId}.",
                appointmentId, recipientUserId);

            await SendWhatsAppAppointmentReminderAsync(appointmentId, recipientUserId, profile.FirstName, profile.LastName, appointment.Provider, appointment.AppointmentDateTime);
        }
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
