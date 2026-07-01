using Rafiq.Domain.Entities;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly RafiqDbContext _dbContext;

    public AuditLogRepository(RafiqDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        => _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken).AsTask();
}
