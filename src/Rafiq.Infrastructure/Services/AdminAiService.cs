using Microsoft.EntityFrameworkCore;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Admin;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Infrastructure.Persistence;

namespace Rafiq.Infrastructure.Services;

public sealed class AdminAiService(RafiqDbContext dbContext) : IAdminAiService
{
    // ── Overview ─────────────────────────────────────────────────────────────

    public async Task<AiOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var today     = DateTime.UtcNow.Date;
        var tomorrow  = today.AddDays(1);
        var week      = today.AddDays(-6);  // 7-day window including today

        // ── Request telemetry ─────────────────────────────────────────────
        var allTimeTotal  = await dbContext.AiRequestLogs.CountAsync(cancellationToken);
        var requestsToday = await dbContext.AiRequestLogs
            .CountAsync(l => l.CreatedAt >= today && l.CreatedAt < tomorrow, cancellationToken);

        // 7-day window — fetch lightweight projection for the trend chart and KPIs
        var recentLogs = await dbContext.AiRequestLogs
            .Where(l => l.CreatedAt >= week && l.CreatedAt < tomorrow)
            .Select(l => new { Date = l.CreatedAt.Date, l.Success, l.DurationMs })
            .ToListAsync(cancellationToken);

        var failedLast7Days = recentLogs.Count(l => !l.Success);

        var avgMs = allTimeTotal > 0
            ? await dbContext.AiRequestLogs.AverageAsync(l => (double?)l.DurationMs, cancellationToken) ?? 0d
            : 0d;

        // ── Reaction data ─────────────────────────────────────────────────
        var allReactions = await dbContext.MessageReactions
            .Select(r => new { Date = r.CreatedAt.Date, r.ReactionType, r.TriageStatus })
            .ToListAsync(cancellationToken);

        var totalThumbsUp   = allReactions.Count(r => r.ReactionType == ReactionType.ThumbsUp);
        var totalThumbsDown = allReactions.Count(r => r.ReactionType == ReactionType.ThumbsDown);
        var totalReactions  = totalThumbsUp + totalThumbsDown;

        var positiveRate = totalReactions > 0
            ? Math.Round(totalThumbsUp * 100m / totalReactions, 1)
            : 100m;

        var unreviewedNegative = allReactions
            .Count(r => r.ReactionType == ReactionType.ThumbsDown && r.TriageStatus == FeedbackStatus.New);

        // ── AI Health Score ───────────────────────────────────────────────
        // Composite: 65% based on 7-day success rate, 35% based on positive feedback rate
        var logs7d         = recentLogs.Count;
        var failed7d       = recentLogs.Count(l => !l.Success);
        var successRate7d  = logs7d > 0 ? (logs7d - failed7d) * 100.0 / logs7d : 100.0;
        var healthScore    = (int)Math.Round(successRate7d * 0.65 + (double)positiveRate * 0.35);

        // ── Conversation counts ───────────────────────────────────────────
        var chatConversations  = await dbContext.AiConversations
            .CountAsync(c => c.Source == AiConversationSource.Chat, cancellationToken);
        var voiceSessions = await dbContext.AiConversations
            .CountAsync(c => c.Source == AiConversationSource.Voice, cancellationToken);

        // ── 7-day quality trend (build day-by-day array) ──────────────────
        var recentReactionsByDay = allReactions
            .Where(r => r.Date >= week && r.Date < tomorrow)
            .GroupBy(r => r.Date)
            .ToDictionary(g => g.Key, g => new
            {
                Up   = g.Count(r => r.ReactionType == ReactionType.ThumbsUp),
                Down = g.Count(r => r.ReactionType == ReactionType.ThumbsDown)
            });

        var logsByDay = recentLogs
            .GroupBy(l => l.Date)
            .ToDictionary(g => g.Key, g => new
            {
                Total  = g.Count(),
                Failed = g.Count(l => !l.Success)
            });

        var qualityTrend = Enumerable.Range(0, 7)
            .Select(i => week.AddDays(i))
            .Select(date =>
            {
                logsByDay.TryGetValue(date, out var log);
                recentReactionsByDay.TryGetValue(date, out var reaction);
                return new AiQualityDayDto(
                    date.ToString("MM/dd"),
                    log?.Total  ?? 0,
                    log?.Failed ?? 0,
                    reaction?.Up   ?? 0,
                    reaction?.Down ?? 0);
            })
            .ToList<AiQualityDayDto>();

        return new AiOverviewDto(
            healthScore,
            positiveRate,
            unreviewedNegative,
            failedLast7Days,
            (int)avgMs,
            requestsToday,
            chatConversations,
            voiceSessions,
            qualityTrend);
    }

    // ── Conversations ─────────────────────────────────────────────────────────

    public async Task<PagedResult<AiRequestListItemDto>> GetRequestsAsync(
        AiRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        var page     = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var logs = dbContext.AiRequestLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Feature) &&
            Enum.TryParse<AiFeature>(query.Feature, ignoreCase: true, out var featureEnum))
            logs = logs.Where(l => l.Feature == featureEnum);

        if (string.Equals(query.Status, "success", StringComparison.OrdinalIgnoreCase))
            logs = logs.Where(l => l.Success);
        else if (string.Equals(query.Status, "failed", StringComparison.OrdinalIgnoreCase))
            logs = logs.Where(l => !l.Success);

        if (query.From.HasValue) logs = logs.Where(l => l.CreatedAt >= query.From.Value);
        if (query.To.HasValue)   logs = logs.Where(l => l.CreatedAt <= query.To.Value);

        var totalCount = await logs.CountAsync(cancellationToken);

        var desc = !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        logs = query.SortBy.ToLowerInvariant() switch
        {
            "duration" => desc ? logs.OrderByDescending(l => l.DurationMs) : logs.OrderBy(l => l.DurationMs),
            "feature"  => desc ? logs.OrderByDescending(l => l.Feature)    : logs.OrderBy(l => l.Feature),
            _          => desc ? logs.OrderByDescending(l => l.CreatedAt)  : logs.OrderBy(l => l.CreatedAt)
        };

        var rawLogs = await logs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = rawLogs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().ToList();
        var userMap = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName, u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var items = rawLogs.Select(l =>
        {
            userMap.TryGetValue(l.UserId ?? Guid.Empty, out var user);
            return new AiRequestListItemDto(
                l.Id, l.UserId, user?.Name, user?.Email,
                l.Feature.ToString(), l.ModelName, l.Success,
                l.ErrorType, l.DurationMs, l.CreatedAt);
        });

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLowerInvariant();
            items = items.Where(i =>
                (i.UserName?.ToLowerInvariant().Contains(s) ?? false)  ||
                (i.UserEmail?.ToLowerInvariant().Contains(s) ?? false) ||
                i.Feature.ToLowerInvariant().Contains(s));
        }

        return new PagedResult<AiRequestListItemDto>(items.ToList(), page, pageSize, totalCount);
    }

    public async Task<AiRequestDetailDto> GetRequestDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var log = await dbContext.AiRequestLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("AiRequestLog", id);

        string? userName = null, userEmail = null;
        if (log.UserId.HasValue)
        {
            var user = await dbContext.Users.AsNoTracking()
                .Where(u => u.Id == log.UserId.Value)
                .Select(u => new { Name = u.FirstName + " " + u.LastName, u.Email })
                .FirstOrDefaultAsync(cancellationToken);
            userName  = user?.Name;
            userEmail = user?.Email;
        }

        string? userPrompt = null, aiResponse = null;
        if (log.ConversationId.HasValue)
        {
            // Fetch the last few messages from the conversation for investigation context
            var messages = await dbContext.AiMessages.AsNoTracking()
                .Where(m => m.AiConversationId == log.ConversationId.Value)
                .OrderByDescending(m => m.SequenceNumber)
                .Take(10)
                .ToListAsync(cancellationToken);

            userPrompt = messages.FirstOrDefault(m => m.Role == AiMessageRole.User)?.Content;
            aiResponse = messages.FirstOrDefault(m => m.Role == AiMessageRole.Assistant)?.Content;
        }

        return new AiRequestDetailDto(
            log.Id, log.Feature.ToString(), log.ModelName,
            log.Success, log.ErrorType, log.DurationMs, log.CreatedAt,
            log.UserId, userName, userEmail,
            log.ConversationId, userPrompt, aiResponse);
    }

    // ── Feedback ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<AiFeedbackListItemDto>> GetFeedbackAsync(
        AiFeedbackQuery query,
        CancellationToken cancellationToken = default)
    {
        var page     = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = dbContext.MessageReactions
            .AsNoTracking()
            .Include(r => r.AiMessage)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Reaction) &&
            Enum.TryParse<ReactionType>(query.Reaction, ignoreCase: true, out var reactionEnum))
            q = q.Where(r => r.ReactionType == reactionEnum);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<FeedbackStatus>(query.Status, ignoreCase: true, out var statusEnum))
            q = q.Where(r => r.TriageStatus == statusEnum);

        if (!string.IsNullOrWhiteSpace(query.Category))
            q = q.Where(r => r.Category == query.Category.Trim());

        if (query.DateFrom.HasValue)
            q = q.Where(r => r.CreatedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            q = q.Where(r => r.CreatedAt <= query.DateTo.Value.AddDays(1));  // inclusive end

        // Apply text search to stored content before pagination
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(r =>
                (r.Feedback != null && r.Feedback.ToLower().Contains(s)) ||
                (r.AiMessage != null && r.AiMessage.Content.ToLower().Contains(s)));
        }

        var totalCount = await q.CountAsync(cancellationToken);

        var rawRows = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = rawRows.Select(r => r.UserId).Distinct().ToList();
        var userMap = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName, u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        // Batch-fetch the preceding user message (the question) for each rated AI message
        var convIds = rawRows
            .Where(r => r.AiMessage != null)
            .Select(r => r.AiMessage!.AiConversationId)
            .Distinct()
            .ToList();

        Dictionary<Guid, List<(int Seq, string? Content)>> userMsgMap = new();
        if (convIds.Count > 0)
        {
            var userMsgs = await dbContext.AiMessages.AsNoTracking()
                .Where(m => convIds.Contains(m.AiConversationId) && m.Role == AiMessageRole.User)
                .Select(m => new { m.AiConversationId, m.SequenceNumber, m.Content })
                .ToListAsync(cancellationToken);

            userMsgMap = userMsgs
                .GroupBy(m => m.AiConversationId)
                .ToDictionary(g => g.Key, g => g.Select(m => (m.SequenceNumber, (string?)m.Content))
                                                 .OrderBy(m => m.Item1).ToList());
        }

        var items = rawRows.Select(r =>
        {
            userMap.TryGetValue(r.UserId, out var user);
            var fullContent = r.AiMessage?.Content ?? string.Empty;
            var excerpt     = fullContent.Length > 80 ? fullContent[..80] + "…" : fullContent;

            string? userPrompt = null;
            if (r.AiMessage != null && userMsgMap.TryGetValue(r.AiMessage.AiConversationId, out var msgs))
            {
                userPrompt = msgs
                    .LastOrDefault(m => m.Seq < r.AiMessage.SequenceNumber)
                    .Content;
            }

            return new AiFeedbackListItemDto(
                r.Id,
                r.UserId,
                user?.Name ?? "Unknown",
                user?.Email ?? string.Empty,
                r.ReactionType.ToString(),
                r.Feedback,
                r.Category,
                r.TriageStatus.ToString(),
                r.AdminNotes,
                excerpt,
                fullContent,
                userPrompt,
                r.AiMessage?.AiConversationId ?? Guid.Empty,
                r.CreatedAt);
        }).ToList();

        return new PagedResult<AiFeedbackListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task UpdateFeedbackAsync(
        Guid id,
        UpdateAiFeedbackDto dto,
        CancellationToken cancellationToken = default)
    {
        var reaction = await dbContext.MessageReactions
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("MessageReaction", id);

        if (!Enum.TryParse<FeedbackStatus>(dto.TriageStatus, ignoreCase: true, out var status))
            throw new BadRequestException($"Unknown triage status: {dto.TriageStatus}");

        reaction.Triage(status, dto.Category, dto.AdminNotes);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // ── Performance ───────────────────────────────────────────────────────────

    public async Task<AiPerformanceDto> GetPerformanceAsync(
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-days + 1);

        var allLogs = await dbContext.AiRequestLogs
            .AsNoTracking()
            .Where(l => l.CreatedAt >= cutoff)
            .Select(l => new { l.Feature, l.Success, l.DurationMs, Date = l.CreatedAt.Date })
            .ToListAsync(cancellationToken);

        var daily = allLogs
            .GroupBy(l => l.Date)
            .OrderBy(g => g.Key)
            .Select(g => new AiDailyStatDto(
                g.Key.ToString("MM/dd"),
                g.Count(),
                g.Count(l => !l.Success),
                g.Any() ? (int)g.Average(l => l.DurationMs) : 0))
            .ToList<AiDailyStatDto>();

        var byFeature = allLogs
            .GroupBy(l => l.Feature.ToString())
            .Select(g => new AdminDistributionItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList<AdminDistributionItemDto>();

        var durations   = allLogs.Select(l => l.DurationMs).OrderBy(d => d).ToList();
        var avgMs       = durations.Count > 0 ? (int)durations.Average() : 0;
        var p95Ms       = durations.Count > 0 ? durations[(int)Math.Floor(durations.Count * 0.95)] : 0;
        var slowReqs    = durations.Count(d => d > 5000);
        var total       = allLogs.Count;
        var failed      = allLogs.Count(l => !l.Success);
        var errorRate   = total == 0 ? 0m : Math.Round(failed * 100m / total, 1);

        return new AiPerformanceDto(daily, byFeature, slowReqs, p95Ms, avgMs, errorRate);
    }

    // ── Insights ──────────────────────────────────────────────────────────────

    public async Task<AiInsightsDto> GetInsightsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-30);

        var byFeature = await dbContext.AiRequestLogs
            .AsNoTracking()
            .Where(l => l.CreatedAt >= cutoff)
            .GroupBy(l => l.Feature)
            .Select(g => new
            {
                Feature      = g.Key,
                Count        = g.Count(),
                FailedCount  = g.Count(l => !l.Success),
                AvgMs        = g.Average(l => (double)l.DurationMs)
            })
            .ToListAsync(cancellationToken);

        var mostUsed     = byFeature.MaxBy(f => f.Count);
        var highestError = byFeature.MaxBy(f => f.FailedCount);
        // Require at least 5 requests for latency rankings to be meaningful
        var qualified    = byFeature.Where(f => f.Count >= 5).ToList();
        var slowest      = qualified.MaxBy(f => f.AvgMs);
        var fastest      = qualified.MinBy(f => f.AvgMs);

        var topCategory = await dbContext.MessageReactions
            .AsNoTracking()
            .Where(r => r.Category != null)
            .GroupBy(r => r.Category)
            .Select(g => new { Category = g.Key!, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync(cancellationToken);

        return new AiInsightsDto(
            mostUsed?.Feature.ToString(),
            mostUsed?.Count ?? 0,
            highestError?.Feature.ToString(),
            highestError?.FailedCount ?? 0,
            slowest?.Feature.ToString(),
            (int)(slowest?.AvgMs ?? 0),
            fastest?.Feature.ToString(),
            (int)(fastest?.AvgMs ?? 0),
            topCategory?.Category,
            topCategory?.Count ?? 0);
    }
}
