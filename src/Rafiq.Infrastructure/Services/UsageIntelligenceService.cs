using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Admin;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Entities.Ai;
using Rafiq.Infrastructure.Persistence;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Services;

public sealed class UsageIntelligenceService(
    RafiqDbContext db,
    INotificationService notificationService,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager,
    ILogger<UsageIntelligenceService> logger) : IUsageIntelligenceService
{
    public async Task<UsageIntelligenceOverviewDto> GetOverviewAsync(CancellationToken ct = default)
    {
        var totalRequests  = await db.AiRequestLogs.CountAsync(ct);
        var flaggedCount   = await db.AiFlaggedRequests.CountAsync(ct);
        var usersToReview  = await db.AiFlaggedRequests
            .Select(f => f.UserId)
            .Distinct()
            .CountAsync(ct);
        var warningsSent   = await db.AiUsageActions
            .Where(a => a.ActionType == "Warning" || a.ActionType == "FinalWarning")
            .CountAsync(ct);

        return new UsageIntelligenceOverviewDto(
            TotalAiRequests:   totalRequests,
            FlaggedRequests:   flaggedCount,
            UsersNeedingReview: usersToReview,
            WarningsSent:      warningsSent);
    }

    public async Task<PagedResult<UsageAttentionUserDto>> GetAttentionQueueAsync(
        UsageAttentionQueueQuery query,
        CancellationToken ct = default)
    {
        var flaggedPerUser = await db.AiFlaggedRequests
            .GroupBy(f => f.UserId)
            .Select(g => new { UserId = g.Key, FlaggedCount = g.Count(), LastActivity = g.Max(f => f.CreatedAt) })
            .OrderByDescending(x => x.FlaggedCount)
            .ToListAsync(ct);

        var totalCount = flaggedPerUser.Count;
        var page  = flaggedPerUser
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var userIds = page.Select(x => x.UserId).ToList();

        var users = await userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.Email, u.ProfileImageUrl })
            .ToListAsync(ct);

        var totalReqPerUser = await db.AiRequestLogs
            .Where(r => r.UserId != null && userIds.Contains(r.UserId.Value))
            .GroupBy(r => r.UserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var warningsPerUser = await db.AiUsageActions
            .Where(a => userIds.Contains(a.TargetUserId)
                     && (a.ActionType == "Warning" || a.ActionType == "FinalWarning"))
            .GroupBy(a => a.TargetUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var items = page.Select(flagged =>
        {
            var user      = users.FirstOrDefault(u => u.Id == flagged.UserId);
            var totalReqs = totalReqPerUser.FirstOrDefault(r => r.UserId == flagged.UserId)?.Count ?? 0;
            var warnings  = warningsPerUser.FirstOrDefault(w => w.UserId == flagged.UserId)?.Count ?? 0;

            return new UsageAttentionUserDto(
                UserId:          flagged.UserId,
                UserName:        user?.UserName ?? flagged.UserId.ToString(),
                UserEmail:       user?.Email ?? "",
                ProfileImageUrl: user?.ProfileImageUrl,
                TotalRequests:   totalReqs,
                FlaggedRequests: flagged.FlaggedCount,
                WarningsSent:    warnings,
                LastActivity:    flagged.LastActivity);
        }).ToList();

        return new PagedResult<UsageAttentionUserDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<UsageUserDetailDto> GetUserDetailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var totalRequests = await db.AiRequestLogs.CountAsync(r => r.UserId == userId, ct);

        var flaggedItems = await db.AiFlaggedRequests
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new UsageFlaggedRequestDto(
                f.Id, f.RequestType, f.UserRequest, f.AiResponse,
                f.Classification, f.Reason, f.CreatedAt))
            .ToListAsync(ct);

        var actionHistory = await db.AiUsageActions
            .Where(a => a.TargetUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new UsageAdminActionDto(a.Id, a.ActionType, a.AdminName, a.Notes, a.CreatedAt))
            .ToListAsync(ct);

        var warningsSent = actionHistory.Count(a =>
            a.ActionType == "Warning" || a.ActionType == "FinalWarning");

        var lastActivity = flaggedItems.Any()
            ? flaggedItems.Max(f => f.CreatedAt)
            : (DateTime?)null;

        return new UsageUserDetailDto(
            UserId:          userId,
            UserName:        user.UserName ?? user.Email ?? userId.ToString(),
            UserEmail:       user.Email ?? "",
            ProfileImageUrl: user.ProfileImageUrl,
            TotalRequests:   totalRequests,
            FlaggedRequests: flaggedItems.Count,
            WarningsSent:    warningsSent,
            LastActivity:    lastActivity,
            IsAiRestricted:  user.IsAiRestricted,
            IsRestricted:    user.IsRestricted,
            IsSuspended:     user.IsSuspended,
            FlaggedItems:    flaggedItems,
            ActionHistory:   actionHistory);
    }

    public async Task TakeActionAsync(
        Guid targetUserId,
        Guid adminId,
        string adminName,
        TakeUsageActionDto dto,
        CancellationToken ct = default)
    {
        var targetUser = await userManager.FindByIdAsync(targetUserId.ToString())
            ?? throw new KeyNotFoundException($"User {targetUserId} not found.");

        // Apply the state change
        var stateChanged = await ApplyActionStateAsync(targetUser, dto.ActionType, targetUserId, ct);

        // Persist action record
        var action = AiUsageAction.Create(targetUserId, adminId, adminName, dto.ActionType, dto.Notes);
        db.AiUsageActions.Add(action);

        // Build notification content (EN + AR)
        var (title, body, titleAr, bodyAr) = dto.ActionType switch
        {
            "Warning"                 => ("⚠️ Usage Warning",
                                         "You have received a warning regarding your AI usage on Rafiq. Please use the AI features for health-related topics only.",
                                         "⚠️ تحذير استخدام",
                                         "لقد تلقيت تحذيرًا بشأن استخدامك للذكاء الاصطناعي على رفيق. يرجى استخدام ميزات الذكاء الاصطناعي للمواضيع الصحية فقط."),
            "FinalWarning"            => ("🚨 Final Warning",
                                         "You have received a final warning regarding your AI usage. Further policy violations may result in access restrictions.",
                                         "🚨 تحذير أخير",
                                         "لقد تلقيت تحذيرًا نهائيًا بشأن استخدامك للذكاء الاصطناعي. قد تؤدي الانتهاكات المستقبلية إلى تقييد وصولك."),
            "RestrictAi"              => ("AI Access Restricted",
                                         "Your AI access has been restricted by an administrator. Please contact support for assistance.",
                                         "تم تقييد وصولك للذكاء الاصطناعي",
                                         "تم تقييد وصولك إلى الذكاء الاصطناعي من قِبَل المسؤول. يرجى التواصل مع الدعم للمساعدة."),
            "RemoveAiRestriction"     => ("AI Access Restored",
                                         "Your AI access has been restored by an administrator.",
                                         "تمت استعادة وصولك للذكاء الاصطناعي",
                                         "تمت استعادة وصولك إلى الذكاء الاصطناعي من قِبَل المسؤول."),
            "RestrictAccount"         => ("Account Restricted",
                                         "Your account has been restricted by an administrator. Please contact support for assistance.",
                                         "تم تقييد حسابك",
                                         "تم تقييد حسابك من قِبَل المسؤول. يرجى التواصل مع الدعم للمساعدة."),
            "RemoveAccountRestriction"=> ("Account Restriction Removed",
                                         "Your account restriction has been removed by an administrator.",
                                         "تمت إزالة قيود الحساب",
                                         "تمت إزالة قيود حسابك من قِبَل المسؤول."),
            "SuspendAccount"          => ("Account Suspended",
                                         "Your account has been suspended by an administrator. Please contact support for further information.",
                                         "تم تعليق حسابك",
                                         "تم تعليق حسابك من قِبَل المسؤول. يرجى التواصل مع الدعم لمزيد من المعلومات."),
            "UnsuspendAccount"        => ("Account Unsuspended",
                                         "Your account has been unsuspended by an administrator. You may now log in again.",
                                         "تمت إعادة تفعيل حسابك",
                                         "تمت إعادة تفعيل حسابك من قِبَل المسؤول. يمكنك تسجيل الدخول مرة أخرى الآن."),
            _                         => ("Administrator Notice",
                                         "You have received a notice from the Rafiq administration team.",
                                         "إشعار من الإدارة",
                                         "لقد تلقيت إشعارًا من فريق إدارة رفيق.")
        };

        db.UserNotifications.Add(new UserNotification(targetUserId, title, body, "admin", titleAr, bodyAr));
        await db.SaveChangesAsync(ct);

        // Push via SignalR (best-effort)
        try
        {
            await notificationService.SendNotificationToUserAsync(targetUserId.ToString(), title, body, ct,
                titleAr: titleAr, bodyAr: bodyAr);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SignalR push failed for admin action on userId={UserId}", targetUserId);
        }

        // Audit trail
        var targetName = targetUser.UserName ?? targetUserId.ToString();
        var severity   = dto.ActionType is "SuspendAccount" or "RestrictAccount" ? "Critical"
                       : dto.ActionType is "FinalWarning"  or "RestrictAi"       ? "Warning"
                       : "Info";

        await auditLogService.LogAsync(
            actorId:     adminId,
            actorName:   adminName,
            actorEmail:  "",
            module:      "AI Operations",
            action:      dto.ActionType,
            target:      targetName,
            severity:    severity,
            description: $"Admin took action '{dto.ActionType}' on user '{targetName}'. Notes: {dto.Notes ?? "—"}",
            changes:     [],
            cancellationToken: ct);
    }

    public async Task SaveFlaggedRequestAsync(AiClassificationContext ctx, CancellationToken ct = default)
    {
        var flag = AiFlaggedRequest.Create(
            ctx.UserId,
            ctx.RequestType,
            ctx.UserRequest,
            ctx.AiResponse,
            ctx.Classification,
            ctx.Reason);

        db.AiFlaggedRequests.Add(flag);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Flagged request saved: userId={UserId}, classification={Class}",
            ctx.UserId, ctx.Classification);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<bool> ApplyActionStateAsync(
        ApplicationUser user, string actionType, Guid userId, CancellationToken ct)
    {
        switch (actionType)
        {
            case "RestrictAi":
                user.IsAiRestricted = true;
                user.UpdatedAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
                await RevokeRefreshTokensAsync(userId, ct);
                logger.LogInformation("AI access restricted for userId={UserId}", userId);
                return true;

            case "RemoveAiRestriction":
                user.IsAiRestricted = false;
                user.UpdatedAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
                logger.LogInformation("AI access restored for userId={UserId}", userId);
                return true;

            case "RestrictAccount":
                user.IsRestricted = true;
                user.UpdatedAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
                await RevokeRefreshTokensAsync(userId, ct);
                logger.LogInformation("Account restricted for userId={UserId}", userId);
                return true;

            case "RemoveAccountRestriction":
                user.IsRestricted = false;
                user.UpdatedAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
                logger.LogInformation("Account restriction removed for userId={UserId}", userId);
                return true;

            case "SuspendAccount":
                user.IsSuspended = true;
                user.UpdatedAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
                await RevokeRefreshTokensAsync(userId, ct);
                logger.LogInformation("Account suspended for userId={UserId}", userId);
                return true;

            case "UnsuspendAccount":
                user.IsSuspended = false;
                user.UpdatedAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
                logger.LogInformation("Account unsuspended for userId={UserId}", userId);
                return true;

            default:
                return false;
        }
    }

    private async Task RevokeRefreshTokensAsync(Guid userId, CancellationToken ct)
    {
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
            token.Revoke();

        if (activeTokens.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
