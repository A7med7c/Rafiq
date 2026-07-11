using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class LabReportRepository : ILabReportRepository
{
    private readonly RafiqDbContext _context;

    public LabReportRepository(RafiqDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(LabReport labReport, CancellationToken cancellationToken = default)
        => _context.LabReports
            .AddAsync(labReport, cancellationToken)
            .AsTask();

    public Task<LabReport?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.LabReports
            .Include(r => r.Results)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LabReport>> GetAllByProfileIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await _context.LabReports
            .Include(r => r.Results)
            .Where(r => r.UserHealthProfileId == userHealthProfileId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    
    public void Update(LabReport labReport)
        => _context.LabReports.Update(labReport);
    
    public void Remove(LabReport labReport)
        => _context.LabReports.Remove(labReport);
}
