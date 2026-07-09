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
        Guid userId,
        CancellationToken cancellationToken = default)
        => context.Appointments
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await context.Appointments
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.AppointmentDateTime)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetUpcomingByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await context.Appointments
            .Where(x => x.UserId == userId &&
                        x.Status == AppointmentStatus.Upcoming &&
                        x.AppointmentDateTime >= DateTime.UtcNow)
            .OrderBy(x => x.AppointmentDateTime)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetTodayByUserIdAsync(
        Guid userId,
        DateTime today,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = today.Date;
        var startOfNextDay = startOfDay.AddDays(1);

        return await context.Appointments
            .Where(x => x.UserId == userId &&
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
        Guid userId,
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
            .Where(x => x.UserId == userId &&
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

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var appointment = await context.Appointments
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (appointment is null)
            return false;

        appointment.SoftDelete();
        return true;
    }
}
