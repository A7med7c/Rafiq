using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence.Identity;
using Rafiq.Infrastructure.Services.Notifications;
using System.Collections.Generic;

namespace Rafiq.Infrastructure.Services.BackgroundJobs;

public sealed class MissedMedicationBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<MissedMedicationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MissedMedicationBackgroundService starting.");

        // Poll cadence — this is what actually determines how soon after a missed stage-3 dose
        // the escalation fires, together with the eligibility cutoff in ProcessMissedMedicationsAsync.
        // Kept at 30s so escalation lands ~1 minute after stage 3 while testing.
        // TODO: raise back to 5 minutes before production to reduce polling load.
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessMissedMedicationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in MissedMedicationBackgroundService tick.");
            }
        }
    }

    private async Task ProcessMissedMedicationsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var logRepository = sp.GetRequiredService<IMedicationReminderLogRepository>();
        var emergencyContactRepository = sp.GetRequiredService<IEmergencyContactRepository>();
        var healthProfileAccessRepository = sp.GetRequiredService<IHealthProfileAccessRepository>();
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
        var whatsAppService = sp.GetRequiredService<IWhatsAppService>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var escalationTemplate = sp.GetRequiredService<IOptions<WhatsAppSettings>>().Value.EscalationTemplate;

        // Stage-3 logs that have been sitting in Sent for >15 minutes without a Confirmed sibling.
        var cutoff = DateTime.UtcNow.AddMinutes(-1); // TODO: change back to -15 before production
        var missedLogs = await logRepository.GetSentStage3LogsOlderThanAsync(cutoff, cancellationToken);

        if (missedLogs.Count == 0)
            return;

        logger.LogInformation("Found {Count} missed medication log(s) to escalate.", missedLogs.Count);

        foreach (var log in missedLogs)
        {
            try
            {
                var profile = log.UserHealthProfile;
                if (profile is null)
                    continue;

                var medicineName = log.MedicineReminder?.UserMedicine?.MedicineName ?? "الدواء";
                var patientName = $"{profile.FirstName} {profile.LastName}";

                // Resolve recipient user IDs.
                var recipientUserIds = new List<Guid>();
                if (profile.UserId is not null)
                {
                    recipientUserIds.Add(profile.UserId.Value);
                }
                else
                {
                    var managementRoles = new[] { AccessRole.Owner, AccessRole.Manager };
                    var members = await healthProfileAccessRepository.GetActiveGranteeUserIdsByRolesAsync(
                        profile.Id,
                        managementRoles,
                        cancellationToken);
                    recipientUserIds.AddRange(members);
                }

                if (recipientUserIds.Count == 0)
                {
                    logger.LogWarning("No active Owners or Managers found for log {LogId}. Skipping escalation.", log.Id);
                    log.MarkAsMissed();
                    logRepository.Update(log);
                    continue;
                }

                // Fetch user details and emergency contacts, deduplicating destinations.
                var phoneToName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var userId in recipientUserIds)
                {
                    var user = await userManager.FindByIdAsync(userId.ToString());
                    if (user is null)
                        continue;

                    var userName = $"{user.FirstName} {user.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(userName))
                        userName = user.UserName ?? "User";

                    if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                    {
                        phoneToName[user.PhoneNumber] = userName;
                    }

                    var contacts = await emergencyContactRepository.GetAllByUserIdAsync(userId, cancellationToken);
                    foreach (var contact in contacts)
                    {
                        if (!string.IsNullOrWhiteSpace(contact.PhoneNumber))
                        {
                            phoneToName[contact.PhoneNumber] = contact.Name;
                        }
                    }
                }

                // Send WhatsApp notifications.
                foreach (var kvp in phoneToName)
                {
                    var phoneNumber = kvp.Key;
                    var recipientName = kvp.Value;

                    await SendSafeAsync(
                        whatsAppService,
                        phoneNumber,
                        escalationTemplate,
                        [recipientName, patientName, medicineName],
                        log.Id,
                        $"recipient '{recipientName}' ({phoneNumber})",
                        logger,
                        cancellationToken);
                }

                // Mark escalated so the next tick ignores this log.
                log.MarkAsMissed();
                logRepository.Update(log);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to escalate missed medication for log {LogId}.", log.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Escalation complete for {Count} missed log(s).", missedLogs.Count);
    }

    private static async Task SendSafeAsync(
        IWhatsAppService whatsAppService,
        string phoneNumber,
        string templateName,
        List<string> parameters,
        Guid logId,
        string recipientLabel,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await whatsAppService.SendTemplateAsync(phoneNumber, templateName, parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "WhatsApp send failed for {Recipient} (log {LogId}).", recipientLabel, logId);
        }
    }
}
