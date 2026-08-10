using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence.Identity;
using Rafiq.Infrastructure.Services.Notifications;

namespace Rafiq.Infrastructure.Services.MedicationReminders;

public sealed class MedicationReminderJob(
    IMedicationReminderLogRepository logRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IWhatsAppService whatsAppService,
    UserManager<ApplicationUser> userManager,
    IOptions<WhatsAppSettings> whatsAppOptions,
    IEmergencyContactRepository emergencyContactRepository,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    ILogger<MedicationReminderJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(Guid logId)
    {
        logger.LogInformation("MedicationReminderJob STARTED for log {LogId}.", logId);

        var log = await logRepository.GetByIdWithDetailsAsync(logId, CancellationToken.None);

        if (log is null)
        {
            logger.LogWarning("MedicationReminderLog {LogId} not found. Skipping.", logId);
            return;
        }

        logger.LogInformation(
            "ReminderLog loaded: status={Status}, reminderNumber={Number}, profileId={ProfileId}, userId={UserId}",
            log.Status, log.ReminderNumber, log.UserHealthProfileId, log.UserHealthProfile?.UserId);

        var reminder = log.MedicineReminder;

        if (reminder is null)
        {
            logger.LogWarning(
                "MedicineReminder for log {LogId} not found. Skipping.", logId);
            return;
        }

        if (reminder.IsDeleted)
        {
            logger.LogInformation(
                "MedicineReminder {ReminderId} has been deleted. Skipping log {LogId}.",
                reminder.Id, logId);
            return;
        }

        if (!reminder.IsEnabled)
        {
            logger.LogInformation(
                "MedicineReminder {ReminderId} is disabled. Skipping log {LogId}.",
                reminder.Id, logId);
            return;
        }

        if (log.Status == MedicationReminderStatus.Cancelled)
        {
            logger.LogInformation(
                "MedicationReminderLog {LogId} is cancelled. Skipping.", logId);
            return;
        }

        if (log.IsCompleted)
        {
            logger.LogInformation(
                "MedicationReminderLog {LogId} is already {Status}. Skipping.",
                logId, log.Status);
            return;
        }

        if (log.Status == MedicationReminderStatus.Snoozed)
        {
            logger.LogInformation(
                "MedicationReminderLog {LogId} is being re-delivered after snooze.", logId);
        }

        logger.LogInformation("Before SendNotificationAsync for log {LogId}.", logId);
        await SendNotificationAsync(log);
        logger.LogInformation("After SendNotificationAsync for log {LogId}.", logId);

        log.MarkAsSent();

        logRepository.Update(log);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation(
            "Sent medication reminder #{Number} for log {LogId}.",
            log.ReminderNumber, logId);
    }

    /// <summary>Formats a <see cref="TimeSpan"/> as "hh:mm AM/PM".</summary>
    private static string FormatTimeSpan(TimeSpan time)
    {
        var totalMinutes = (int)time.TotalMinutes;
        var h = (totalMinutes / 60) % 24;
        var m = totalMinutes % 60;
        var period = h >= 12 ? "PM" : "AM";
        var hour12 = h % 12 == 0 ? 12 : h % 12;
        return $"{hour12:D2}:{m:D2} {period}";
    }

    private async Task<bool> IsFamilyMemberProfileAsync(UserHealthProfile profile)
    {
        if (profile.UserId is null)
            return true;

        var activeMembers = await healthProfileAccessRepository.GetActiveMembersAsync(profile.Id, CancellationToken.None);
        return activeMembers.Any(a => a.GranteeUserId != profile.UserId.Value);
    }

    private async Task<List<ApplicationUser>> GetProfileManagersAsync(UserHealthProfile profile)
    {
        var activeMembers = await healthProfileAccessRepository.GetActiveMembersAsync(profile.Id, CancellationToken.None);
        var users = new List<ApplicationUser>();

        foreach (var access in activeMembers)
        {
            // Viewers have read-only access and do not manage the family member profile,
            // so they must not receive WhatsApp medication reminders or escalations.
            if (access.Role == AccessRole.Viewer)
                continue;

            var user = await userManager.FindByIdAsync(access.GranteeUserId.ToString());
            if (user is not null && !users.Any(u => u.Id == user.Id))
            {
                users.Add(user);
            }
        }

        if (profile.UserId is not null && !users.Any(u => u.Id == profile.UserId.Value))
        {
            var user = await userManager.FindByIdAsync(profile.UserId.Value.ToString());
            if (user is not null)
            {
                users.Add(user);
            }
        }

        return users;
    }

    private async Task SendNotificationAsync(MedicationReminderLog log)
    {
        var profile = log.UserHealthProfile;
        if (profile is null)
        {
            logger.LogWarning("Log {LogId} has no associated UserHealthProfile. Skipping.", log.Id);
            return;
        }

        var medicineName = log.MedicineReminder?.UserMedicine?.MedicineName ?? "your medication";
        var dosage = log.MedicineReminder?.UserMedicine?.Dosage ?? string.Empty;

        var message = log.ReminderNumber switch
        {
            1 => $"{medicineName} ({dosage}) is due soon. Get ready to take it.",
            2 => $"Time to take {medicineName} ({dosage}). Confirm once taken.",
            _ => $"{medicineName} ({dosage}) is overdue. Please confirm if taken.",
        };

        var configuredTime = log.MedicineReminder?.ReminderTime ?? log.ScheduledTime;
        var reminderTimeFormatted = FormatTimeSpan(configuredTime);

        var payload = new MedicationReminderNotificationPayload
        {
            ReminderId = log.Id.ToString(),
            MedicineId = log.MedicineReminder?.UserMedicineId.ToString() ?? string.Empty,
            MedicineName = medicineName,
            GenericName = string.Empty,
            Strength = dosage,
            Dosage = dosage,
            ReminderTime = reminderTimeFormatted,
            Status = log.Status.ToString(),
            NotificationText = message
        };

        var targetUsers = await GetProfileManagersAsync(profile);

        foreach (var targetUser in targetUsers)
        {
            logger.LogInformation(
                "Before notificationService.SendMedicationReminderAsync: targetUserId='{UserId}', logId={LogId}",
                targetUser.Id, log.Id);

            await notificationService.SendMedicationReminderAsync(
                targetUser.Id.ToString(),
                payload,
                CancellationToken.None);
        }

        if (log.ReminderNumber == 2)
        {
            await SendWhatsAppMedicineReminderAsync(log, medicineName);
        }

        if (log.ReminderNumber == 3)
        {
            await SendWhatsAppEmergencyReminderAsync(log, medicineName);
        }
    }

    private async Task SendWhatsAppMedicineReminderAsync(
        MedicationReminderLog log,
        string medicineName)
    {
        var profile = log.UserHealthProfile;
        var patientName = $"{profile.FirstName} {profile.LastName}".Trim();

        // 1. Deliver primary dose-time reminder (medicine_now_reminder) to the patient themselves (if registered user)
        if (profile.UserId is not null)
        {
            var patientUser = await userManager.FindByIdAsync(profile.UserId.Value.ToString());
            if (patientUser is not null && !string.IsNullOrWhiteSpace(patientUser.PhoneNumber))
            {
                var primaryTemplate = whatsAppOptions.Value.PrimaryReminderTemplate;
                logger.LogInformation(
                    "Sending WhatsApp primary template '{Template}' to patient '{Phone}' for log {LogId}.",
                    primaryTemplate, patientUser.PhoneNumber, log.Id);

                try
                {
                    await whatsAppService.SendTemplateAsync(
                        patientUser.PhoneNumber,
                        primaryTemplate,
                        [patientName, medicineName],
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to send WhatsApp primary reminder for log {LogId} to patient {UserId}.",
                        log.Id, patientUser.Id);
                }
            }
            else
            {
                logger.LogWarning(
                    "Patient user {UserId} has no phone number registered for log {LogId}.",
                    profile.UserId.Value, log.Id);
            }
        }

        // 2. Deliver family alert template (emergency) to Managers/Owners managing this profile
        var managers = await GetProfileManagersAsync(profile);
        logger.LogInformation("Found {Count} managers for profile {ProfileId}.", managers.Count, profile.Id);

        foreach (var manager in managers)
        {
            // Skip sending the manager "emergency" template to the patient themselves (patient received medicine_now_reminder above)
            if (profile.UserId is not null && manager.Id == profile.UserId.Value)
                continue;

            if (string.IsNullOrWhiteSpace(manager.PhoneNumber))
            {
                logger.LogWarning("No phone number for manager {UserId} ({UserName}). Skipping family reminder #2 for log {LogId}.", manager.Id, manager.UserName, log.Id);
                continue;
            }

            var familyTemplate = whatsAppOptions.Value.FamilyReminderTemplate;
            logger.LogInformation(
                "Sending WhatsApp family template '{Template}' to manager '{Phone}' for log {LogId}.",
                familyTemplate, manager.PhoneNumber, log.Id);

            try
            {
                await whatsAppService.SendTemplateAsync(
                    manager.PhoneNumber,
                    familyTemplate,
                    [],
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send WhatsApp family reminder #2 for log {LogId} to manager {UserId}.",
                    log.Id, manager.Id);
            }
        }
    }

    private async Task SendWhatsAppEmergencyReminderAsync(
        MedicationReminderLog log,
        string medicineName)
    {
        var profile = log.UserHealthProfile;
        var patientName = $"{profile.FirstName} {profile.LastName}".Trim();
        var escalationTemplate = whatsAppOptions.Value.EscalationTemplate;

        // 1. Deliver escalation template (emergency_reminder) to the patient themselves (if registered user)
        if (profile.UserId is not null)
        {
            var patientUser = await userManager.FindByIdAsync(profile.UserId.Value.ToString());
            if (patientUser is not null && !string.IsNullOrWhiteSpace(patientUser.PhoneNumber))
            {
                logger.LogInformation(
                    "Sending WhatsApp escalation template '{Template}' to patient '{Phone}' for log {LogId}.",
                    escalationTemplate, patientUser.PhoneNumber, log.Id);

                try
                {
                    await whatsAppService.SendTemplateAsync(
                        patientUser.PhoneNumber,
                        escalationTemplate,
                        [patientName, patientName, medicineName],
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to send WhatsApp escalation to patient {UserId} for log {LogId}.",
                        patientUser.Id, log.Id);
                }
            }
            else
            {
                logger.LogWarning(
                    "Patient user {UserId} has no phone number registered for escalation #3 for log {LogId}.",
                    profile.UserId.Value, log.Id);
            }
        }

        // 2. Deliver escalation template (emergency_reminder) to Managers/Owners managing this profile
        var managers = await GetProfileManagersAsync(profile);
        logger.LogInformation("Found {Count} managers for profile {ProfileId} in escalation #3.", managers.Count, profile.Id);

        foreach (var manager in managers)
        {
            // Skip sending manager template to the patient themselves (patient received patient escalation in Step 1 above)
            if (profile.UserId is not null && manager.Id == profile.UserId.Value)
                continue;

            if (string.IsNullOrWhiteSpace(manager.PhoneNumber))
            {
                logger.LogWarning("No phone number for manager {UserId} ({UserName}). Skipping family escalation #3 for log {LogId}.", manager.Id, manager.UserName, log.Id);
                continue;
            }

            var managerName = !string.IsNullOrWhiteSpace(manager.FirstName)
                ? manager.FirstName
                : (!string.IsNullOrWhiteSpace(manager.LastName) ? manager.LastName : "مستخدم");

            logger.LogInformation(
                "Sending WhatsApp family escalation template '{Template}' to manager '{Phone}' for log {LogId}. Parameters: [{ManagerName}, {PatientName}, {MedicineName}]",
                escalationTemplate, manager.PhoneNumber, log.Id, managerName, patientName, medicineName);

            try
            {
                await whatsAppService.SendTemplateAsync(
                    manager.PhoneNumber,
                    escalationTemplate,
                    [managerName, patientName, medicineName],
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send WhatsApp family escalation to manager {UserId} for log {LogId}.",
                    manager.Id, log.Id);
            }
        }

        // 3. Deliver escalation template (emergency_reminder) to Emergency Contacts
        var userIdsForContacts = new List<Guid>();
        if (profile.UserId is not null)
        {
            userIdsForContacts.Add(profile.UserId.Value);
        }
        foreach (var manager in managers)
        {
            if (!userIdsForContacts.Contains(manager.Id))
            {
                userIdsForContacts.Add(manager.Id);
            }
        }

        foreach (var userId in userIdsForContacts)
        {
            IReadOnlyList<EmergencyContact> contacts;
            try
            {
                contacts = await emergencyContactRepository.GetAllByUserIdAsync(userId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve emergency contacts for user {UserId}.", userId);
                continue;
            }

            logger.LogInformation("Found {Count} emergency contacts for user {UserId}.", contacts.Count, userId);

            foreach (var contact in contacts)
            {
                if (string.IsNullOrWhiteSpace(contact.PhoneNumber))
                {
                    logger.LogWarning("Emergency contact {ContactId} ({ContactName}) has empty phone number. Skipping.", contact.Id, contact.Name);
                    continue;
                }

                logger.LogInformation(
                    "Sending WhatsApp escalation template '{Template}' to emergency contact '{Phone}' ({ContactName}) for log {LogId}.",
                    escalationTemplate, contact.PhoneNumber, contact.Name, log.Id);

                try
                {
                    await whatsAppService.SendTemplateAsync(
                        contact.PhoneNumber,
                        escalationTemplate,
                        [contact.Name, patientName, medicineName],
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send WhatsApp escalation to contact '{Phone}'.", contact.PhoneNumber);
                }
            }
        }
    }
}
