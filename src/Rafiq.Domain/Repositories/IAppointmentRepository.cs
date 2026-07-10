using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Repositories;

public interface IAppointmentRepository
{
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetAllByUserHealthProfileIdAsync(Guid userHealthProfileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetUpcomingByUserHealthProfileIdAsync(Guid userHealthProfileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetTodayByUserHealthProfileIdAsync(Guid userHealthProfileId, DateTime today, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetExpiredUpcomingAppointmentsAsync(DateTime referenceTime, CancellationToken cancellationToken = default);

    Task<bool> ExistsDuplicateAsync(
        Guid userHealthProfileId,
        AppointmentType appointmentType,
        string title,
        string provider,
        DateTime appointmentDateTime,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
