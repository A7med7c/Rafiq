using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Services.BackgroundJobs;

public class MissedAppointmentsBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<MissedAppointmentsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MissedAppointmentsBackgroundService is starting.");

        // Run every 15 minutes
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessMissedAppointmentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing MissedAppointmentsBackgroundService.");
            }
        }
    }

    private async Task ProcessMissedAppointmentsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var appointmentRepository = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // An appointment is missed if 3 hours have passed since its start time.
        var referenceTime = DateTime.UtcNow.AddHours(-3);
        
        var expiredAppointments = await appointmentRepository.GetExpiredUpcomingAppointmentsAsync(referenceTime, cancellationToken);

        if (!expiredAppointments.Any())
            return;

        int count = 0;
        foreach (var appointment in expiredAppointments)
        {
            try
            {
                appointment.MarkAsMissed();
                count++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to mark appointment {AppointmentId} as missed.", appointment.Id);
            }
        }

        if (count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Marked {Count} expired appointments as missed.", count);
        }
    }
}
