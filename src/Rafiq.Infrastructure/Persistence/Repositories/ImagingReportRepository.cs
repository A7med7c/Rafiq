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
        CancellationToken cancellationToken = default)
        => _context.ImagingReports
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ImagingReport>> GetAllByProfileIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await _context.ImagingReports
            .Where(r => r.UserHealthProfileId == userHealthProfileId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
}
