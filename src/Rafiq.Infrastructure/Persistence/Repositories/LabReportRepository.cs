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
        Guid userId,
        CancellationToken cancellationToken = default)
        => _context.LabReports
            .Include(r => r.Results)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<LabReport>> GetAllByUserIdAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
    => await _context.LabReports
        .Include(r => r.Results)
        .Where(r => r.UserId == userId)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync(cancellationToken);

    /// <summary>
    /// Looks up the DocumentType by name.
    /// If it does not exist yet, creates it and adds it to the change tracker.
    /// It will be persisted together with the LabReport in the same SaveChangesAsync call.
    /// </summary>
    public async Task<Guid> GetOrCreateDocumentTypeIdAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.DocumentTypes
            .FirstOrDefaultAsync(dt => dt.Name == name, cancellationToken);

        if (existing is not null)
            return existing.Id;

        var newDocType = new DocumentType(name, $"Medical document type: {name}");
        await _context.DocumentTypes.AddAsync(newDocType, cancellationToken);
        return newDocType.Id;
    }
}
