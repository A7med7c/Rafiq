using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class ImagingReportRepository : IImagingReportRepository
{
    private readonly RafiqDbContext _context;

    public ImagingReportRepository(RafiqDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(ImagingReport imagingReport, CancellationToken cancellationToken = default)
        => _context.ImagingReports
            .AddAsync(imagingReport, cancellationToken)
            .AsTask();

    public Task<ImagingReport?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
        => _context.ImagingReports
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<ImagingReport>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _context.ImagingReports
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
}
