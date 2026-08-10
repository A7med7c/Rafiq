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
using Rafiq.Domain.Entities.User;

namespace Rafiq.Infrastructure.Services.AppointmentReminders;

public sealed class AppointmentReminderJob(
    IAppointmentRepository appointmentRepository,
    INotificationService notificationService,
    IWhatsAppService whatsAppService,
    IEmergencyContactRepository emergencyContactRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
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

        var signalRRecipients = new HashSet<Guid>();
        if (profile.UserId.HasValue)
        {
            signalRRecipients.Add(profile.UserId.Value);
        }

        var familyMembers = await healthProfileAccessRepository.GetActiveMembersAsync(appointment.UserHealthProfileId, CancellationToken.None);
        foreach (var member in familyMembers)
        {
            if (member.Role == AccessRole.Owner || member.Role == AccessRole.Manager)
            {
                signalRRecipients.Add(member.GranteeUserId);
            }
        }

        foreach (var recipientId in signalRRecipients)
        {
            await notificationService.SendAppointmentReminderAsync(
                recipientId.ToString(),
                payload,
                CancellationToken.None);

            logger.LogInformation(
                "Sent appointment reminder for {AppointmentId} to user {UserId}.",
                appointmentId, recipientId);
        }

        await SendWhatsAppRemindersSafeAsync(appointment, profile.UserId, familyMembers);
    }

    private async Task SendWhatsAppRemindersSafeAsync(Appointment appointment, Guid? patientUserId, IReadOnlyList<HealthProfileAccess> familyMembers)
    {
        try
        {
            var patientName = $"{appointment.UserHealthProfile.FirstName} {appointment.UserHealthProfile.LastName}";
            var utcTime = DateTime.SpecifyKind(appointment.AppointmentDateTime, DateTimeKind.Utc);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, dateTimeProvider.ReminderTimeZone);
            var appointmentDate = localTime.ToString("dd/MM/yyyy");
            var appointmentTime = localTime.ToString("h:mm tt");
            var templateName = whatsAppOptions.Value.AppointmentReminderTemplate;
            var familyTemplateName = whatsAppOptions.Value.FamilyAppointmentReminderTemplate;

            var notifiedPhoneNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Send to patient
            if (patientUserId.HasValue)
            {
                var user = await userManager.FindByIdAsync(patientUserId.Value.ToString());
                if (user != null && !string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    await SendSafeAsync(
                        user.PhoneNumber, 
                        templateName, 
                        [patientName, appointment.Provider, appointmentDate, appointmentTime], 
                        "patient");
                    notifiedPhoneNumbers.Add(user.PhoneNumber);
                }
            }

            // Send to eligible Family Members
            foreach (var member in familyMembers)
            {
                if (member.Role == AccessRole.Owner || member.Role == AccessRole.Manager)
                {
                    var familyUser = await userManager.FindByIdAsync(member.GranteeUserId.ToString());
                    if (familyUser != null && !string.IsNullOrWhiteSpace(familyUser.PhoneNumber))
                    {
                        if (notifiedPhoneNumbers.Contains(familyUser.PhoneNumber))
                            continue;

                        var familyMemberName = $"{familyUser.FirstName} {familyUser.LastName}".Trim();
                        if (string.IsNullOrWhiteSpace(familyMemberName))
                            familyMemberName = "Family Member";

                        await SendSafeAsync(
                            familyUser.PhoneNumber, 
                            familyTemplateName, 
                            [familyMemberName, patientName, appointment.Provider, appointmentDate, appointmentTime], 
                            $"family member ({member.Role}) '{familyMemberName}'");

                        notifiedPhoneNumbers.Add(familyUser.PhoneNumber);
                    }
                }
            }

            // Send to emergency contacts
            if (patientUserId.HasValue)
            {
                var emergencyContacts = await emergencyContactRepository.GetAllByUserIdAsync(patientUserId.Value, CancellationToken.None);
                foreach (var contact in emergencyContacts)
                {
                    if (!string.IsNullOrWhiteSpace(contact.PhoneNumber))
                    {
                        if (notifiedPhoneNumbers.Contains(contact.PhoneNumber))
                            continue;

                        await SendSafeAsync(
                            contact.PhoneNumber, 
                            familyTemplateName, 
                            [contact.Name, patientName, appointment.Provider, appointmentDate, appointmentTime], 
                            $"emergency contact '{contact.Name}'");

                        notifiedPhoneNumbers.Add(contact.PhoneNumber);
                    }
                }
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
