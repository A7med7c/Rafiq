using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Repositories;

public interface IAppointmentRepository
{
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task<Appointment?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetUpcomingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetTodayByUserIdAsync(Guid userId, DateTime today, CancellationToken cancellationToken = default);

    Task<bool> ExistsDuplicateAsync(
        Guid userId,
        AppointmentType appointmentType,
        string title,
        string provider,
        DateTime appointmentDateTime,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
