using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence.Identity;
using Rafiq.Infrastructure.Services.Notifications;

namespace Rafiq.Infrastructure.Services.BackgroundJobs;

public sealed class MissedMedicationBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<MissedMedicationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MissedMedicationBackgroundService starting.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

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

                if (profile?.UserId is null)
                {
                    logger.LogWarning(
                        "Log {LogId} belongs to a managed profile with no account. Skipping escalation.",
                        log.Id);
                    // Still mark as Missed so this log is not revisited on the next tick.
                    log.MarkAsMissed();
                    logRepository.Update(log);
                    continue;
                }

                var medicineName = log.MedicineReminder?.UserMedicine?.MedicineName ?? "الدواء";
                var patientName = $"{profile.FirstName} {profile.LastName}";

                var user = await userManager.FindByIdAsync(profile.UserId.Value.ToString());

                // Notify the patient.
                if (user?.PhoneNumber is not null)
                {
                    await SendSafeAsync(
                        whatsAppService,
                        user.PhoneNumber,
                        escalationTemplate,
                        [patientName, patientName, medicineName],
                        log.Id,
                        "patient",
                        logger,
                        cancellationToken);
                }
                else
                {
                    logger.LogWarning(
                        "No phone number for user {UserId}. Patient WhatsApp skipped for log {LogId}.",
                        profile.UserId.Value, log.Id);
                }

                // Notify each emergency contact.
                var emergencyContacts = await emergencyContactRepository
                    .GetAllByUserIdAsync(profile.UserId.Value, cancellationToken);

                foreach (var contact in emergencyContacts)
                {
                    await SendSafeAsync(
                        whatsAppService,
                        contact.PhoneNumber,
                        escalationTemplate,
                        [contact.Name, patientName, medicineName],
                        log.Id,
                        $"emergency contact '{contact.Name}'",
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
