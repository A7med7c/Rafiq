using Microsoft.Extensions.Logging;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Services.Appointments;

/// <summary>
/// Recurring Hangfire job that marks past-due appointments as Missed.
/// This logic was previously executed lazily inside GET query handlers, which made
/// those endpoints non-read-only. Moving it here keeps all GET endpoints pure reads
/// while still ensuring appointment statuses are kept accurate.
///
/// Registered in Program.cs to run every hour (or at a frequency appropriate for
/// your SLA). Running more frequently than the appointment granularity is harmless —
/// the underlying UpdateMissedAppointmentsAsync is idempotent.
/// </summary>
public sealed class UpdateMissedAppointmentsJob(
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateMissedAppointmentsJob> logger)
{
    public async Task ExecuteAsync()
    {
        await appointmentRepository.UpdateMissedAppointmentsAsync(CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation("UpdateMissedAppointmentsJob completed at {UtcNow:o}.", DateTime.UtcNow);
    }
}
