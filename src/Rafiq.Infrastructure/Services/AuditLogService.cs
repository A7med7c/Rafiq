using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Admin;
using Rafiq.Domain.Entities;
using Rafiq.Infrastructure.Persistence;

namespace Rafiq.Infrastructure.Services;

public sealed class AuditLogService(RafiqDbContext dbContext) : IAuditLogService
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task LogAsync(
        Guid actorId,
        string actorName,
        string actorEmail,
        string module,
        string action,
        string target,
        string severity,
        string description,
        IReadOnlyList<(string Field, string? Before, string? After)> changes,
        CancellationToken cancellationToken = default)
    {
        var changesJson = changes.Count > 0
            ? JsonSerializer.Serialize(
                changes.Select(c => new { field = c.Field, before = c.Before, after = c.After }),
                _json)
            : null;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorId     = actorId,
            ActorName   = actorName,
            ActorEmail  = actorEmail,
            Module      = module,
            Action      = action,
            Target      = target,
            Severity    = severity,
            Description = description,
            ChangesJson = changesJson
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var page     = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var logs = dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            logs = logs.Where(l =>
                l.ActorName.Contains(s) ||
                l.Action.Contains(s) ||
                l.Target.Contains(s) ||
                l.Description.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(query.Module))
            logs = logs.Where(l => l.Module == query.Module);

        if (!string.IsNullOrWhiteSpace(query.Severity))
            logs = logs.Where(l => l.Severity == query.Severity);

        if (!string.IsNullOrWhiteSpace(query.DateFrom) &&
            DateTime.TryParse(query.DateFrom, out var from))
            logs = logs.Where(l => l.Timestamp >= from);

        if (!string.IsNullOrWhiteSpace(query.DateTo) &&
            DateTime.TryParse(query.DateTo, out var to))
            logs = logs.Where(l => l.Timestamp <= to.Date.AddDays(1));

        var total = await logs.CountAsync(cancellationToken);

        var rows = await logs
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(l => new AuditLogDto(
            l.Id,
            l.Timestamp,
            l.ActorName,
            l.ActorEmail,
            l.Module,
            l.Action,
            l.Target,
            l.Severity,
            l.Description,
            ParseChanges(l.ChangesJson)
        )).ToList();

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }

    private static IReadOnlyList<AuditChangeDto> ParseChanges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var raw = JsonSerializer.Deserialize<List<JsonChangeEntry>>(json, _json);
            return raw?.Select(e => new AuditChangeDto(e.Field, e.Before, e.After)).ToList()
                   ?? [];
        }
        catch { return []; }
    }

    private sealed record JsonChangeEntry(string Field, string? Before, string? After);
}
