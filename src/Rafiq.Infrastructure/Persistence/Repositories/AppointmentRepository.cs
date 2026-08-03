using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class AppointmentRepository(RafiqDbContext context) : IAppointmentRepository
{
    public Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
        => context.Appointments.AddAsync(appointment, cancellationToken).AsTask();

    public Task<Appointment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => context.Appointments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Appointment?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => context.Appointments
            .Include(x => x.UserHealthProfile)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetAllByUserHealthProfileIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await context.Appointments
            .Where(x => x.UserHealthProfileId == userHealthProfileId)
            .OrderBy(x => x.AppointmentDateTime)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetUpcomingByUserHealthProfileIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await context.Appointments
            .Where(x => x.UserHealthProfileId == userHealthProfileId &&
                        x.Status == AppointmentStatus.Upcoming &&
                        x.AppointmentDateTime >= DateTime.UtcNow)
            .OrderBy(x => x.AppointmentDateTime)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetTodayByUserHealthProfileIdAsync(
        Guid userHealthProfileId,
        DateTime today,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = today.Date;
        var startOfNextDay = startOfDay.AddDays(1);

        return await context.Appointments
            .Where(x => x.UserHealthProfileId == userHealthProfileId &&
                        x.Status == AppointmentStatus.Upcoming &&
                        x.AppointmentDateTime >= startOfDay &&
                        x.AppointmentDateTime < startOfNextDay)
            .OrderBy(x => x.AppointmentDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetExpiredUpcomingAppointmentsAsync(
        DateTime referenceTime,
        CancellationToken cancellationToken = default)
        => await context.Appointments
            .Where(x => x.Status == AppointmentStatus.Upcoming &&
                        x.AppointmentDateTime <= referenceTime)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsDuplicateAsync(
        Guid userHealthProfileId,
        AppointmentType appointmentType,
        string title,
        string provider,
        DateTime appointmentDateTime,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToUpper();
        var normalizedProvider = provider.Trim().ToUpper();

        var query = context.Appointments
            .Where(x => x.UserHealthProfileId == userHealthProfileId &&
                        x.AppointmentType == appointmentType &&
                        x.AppointmentDateTime == appointmentDateTime &&
                        x.Title.Trim().ToUpper() == normalizedTitle &&
                        x.Provider.Trim().ToUpper() == normalizedProvider);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task UpdateMissedAppointmentsAsync(CancellationToken cancellationToken)
    {


        var now = DateTime.UtcNow;

        Console.WriteLine($"UTC NOW = {now}");

        var appointments = await context.Appointments
            .Where(a => a.Status == AppointmentStatus.Upcoming)
            .ToListAsync(cancellationToken);

        foreach (var appointment in appointments)
        {
            Console.WriteLine(
                $"{appointment.Title} | {appointment.AppointmentDateTime} | {(appointment.AppointmentDateTime <= now)}");
            var appointmentUtc = DateTime.SpecifyKind(
    appointment.AppointmentDateTime,
    DateTimeKind.Utc);

            if (appointment.AppointmentDateTime <= now)
            {
                appointment.MarkAsMissed();

            }
        }
    }
}
