using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class ImagingReportRepository(RafiqDbContext context) : IImagingReportRepository
{
    public Task AddAsync(ImagingReport imagingReport, CancellationToken cancellationToken = default)
        => context.ImagingReports
            .AddAsync(imagingReport, cancellationToken)
            .AsTask();

    public Task<ImagingReport?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
        => context.ImagingReports
            .FirstOrDefaultAsync(r => r.ReportId == id && r.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<ImagingReport>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await context.ImagingReports
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReportDate)
            .ToListAsync(cancellationToken);
}
