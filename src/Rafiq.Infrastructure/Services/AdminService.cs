using Microsoft.EntityFrameworkCore;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Admin;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Infrastructure.Persistence;

namespace Rafiq.Infrastructure.Services;

public sealed class AdminService(RafiqDbContext dbContext, IAuditLogService auditLog) : IAdminService
{
    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);
        var previousMonthStart = monthStart.AddMonths(-1);
        var todayOnly = DateOnly.FromDateTime(now);

        var totalUsers = await dbContext.Users.CountAsync(user => !user.IsDeleted, cancellationToken);
        var activeUsers = await dbContext.Users.CountAsync(
            user => !user.IsDeleted && user.IsActive,
            cancellationToken);
        var totalProfiles = await dbContext.UserHealthProfiles.CountAsync(cancellationToken);
        var managedProfiles = await dbContext.UserHealthProfiles.CountAsync(
            profile => profile.UserId == null,
            cancellationToken);
        var totalAiRequests = await dbContext.AiRequestLogs.CountAsync(cancellationToken);
        var flaggedAiRequests = await dbContext.AiFlaggedRequests.CountAsync(cancellationToken);
        
        var mostActiveAiUserGroup = await dbContext.AiRequestLogs
            .Where(r => r.UserId != null)
            .GroupBy(r => r.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .FirstOrDefaultAsync(cancellationToken);

        string? mostActiveAiUser = null;
        if (mostActiveAiUserGroup?.UserId != null)
        {
            var topUser = await dbContext.Users.FindAsync(mostActiveAiUserGroup.UserId);
            mostActiveAiUser = topUser != null ? topUser.FirstName + " " + topUser.LastName : null;
        }
        var remindersToday = await dbContext.MedicationReminderLogs.CountAsync(
            reminder => reminder.ScheduledDate == todayOnly,
            cancellationToken);
        var prescriptions = await dbContext.Prescriptions.CountAsync(cancellationToken);
        var labReports = await dbContext.LabReports.CountAsync(cancellationToken);
        var imagingReports = await dbContext.ImagingReports.CountAsync(cancellationToken);
        var generalDocuments = await dbContext.GeneralDocuments.CountAsync(cancellationToken);
        var aiConversations = await dbContext.AiConversations.CountAsync(cancellationToken);
        var currentRegistrations = await dbContext.Users.CountAsync(
            user => !user.IsDeleted
                && user.CreatedAt >= monthStart
                && user.CreatedAt < nextMonthStart,
            cancellationToken);
        var previousRegistrations = await dbContext.Users.CountAsync(
            user => !user.IsDeleted
                && user.CreatedAt >= previousMonthStart
                && user.CreatedAt < monthStart,
            cancellationToken);

        var growthStart = monthStart.AddMonths(-5);
        var userGrowthRows = await dbContext.Users
            .AsNoTracking()
            .Where(user => !user.IsDeleted && user.CreatedAt >= growthStart)
            .GroupBy(user => new { user.CreatedAt.Year, user.CreatedAt.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var userGrowth = BuildSixMonthTrend(
            monthStart,
            userGrowthRows.ToDictionary(row => (row.Year, row.Month), row => row.Count));

        // EF Core cannot translate enum.ToString() inside a SQL GroupBy projection.
        // Pull the grouped counts into memory first, then convert enum keys to strings.
        var genderGroups = await dbContext.UserHealthProfiles
            .AsNoTracking()
            .Where(profile => !profile.IsDeleted)
            .GroupBy(profile => profile.Gender)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var genderDistribution = genderGroups
            .Select(g => new AdminDistributionItemDto(g.Key.ToString(), g.Count))
            .OrderByDescending(item => item.Value)
            .ToList<AdminDistributionItemDto>();

        var recentUsers = await dbContext.Users
            .AsNoTracking()
            .Where(user => !user.IsDeleted)
            .OrderByDescending(user => user.CreatedAt)
            .Take(6)
            .Select(user => new AdminRecentUserDto(
                user.Id,
                user.FirstName + " " + user.LastName,
                user.Email ?? string.Empty,
                user.IsActive,
                user.CreatedAt,
                user.ProfileImageUrl))
            .ToListAsync(cancellationToken);

        var usersNeedAttentionGroups = await dbContext.AiFlaggedRequests
            .AsNoTracking()
            .GroupBy(f => f.UserId)
            .Select(g => new { UserId = g.Key, FlaggedCount = g.Count() })
            .OrderByDescending(x => x.FlaggedCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        var attentionUserIds = usersNeedAttentionGroups.Select(x => x.UserId).ToList();
        var attentionUsersData = await dbContext.Users
            .AsNoTracking()
            .Where(u => attentionUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.ProfileImageUrl })
            .ToListAsync(cancellationToken);

        var usersNeedAttention = usersNeedAttentionGroups
            .Select(g => {
                var u = attentionUsersData.FirstOrDefault(x => x.Id == g.UserId);
                return new AdminDashboardAttentionUserDto(
                    g.UserId,
                    u != null ? $"{u.FirstName} {u.LastName}" : "Unknown",
                    u?.ProfileImageUrl,
                    $"{g.FlaggedCount} Flagged Requests"
                );
            })
            .ToList<AdminDashboardAttentionUserDto>();

        var monthlyGrowth = previousRegistrations == 0
            ? currentRegistrations > 0 ? 100m : 0m
            : Math.Round(
                (currentRegistrations - previousRegistrations) * 100m / previousRegistrations,
                1);

        return new AdminDashboardDto(
            totalUsers,
            activeUsers,
            totalProfiles,
            managedProfiles,
            remindersToday,
            prescriptions + labReports + imagingReports + generalDocuments,
            aiConversations,
            totalAiRequests,
            flaggedAiRequests,
            mostActiveAiUser,
            currentRegistrations,
            monthlyGrowth,
            userGrowth,
            genderDistribution,
            recentUsers,
            usersNeedAttention);
    }

    public async Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var users = dbContext.Users
            .AsNoTracking()
            .Where(user => !user.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(user =>
                user.FirstName.Contains(search)
                || user.LastName.Contains(search)
                || (user.Email != null && user.Email.Contains(search))
                || (user.PhoneNumber != null && user.PhoneNumber.Contains(search)));
        }

        if (string.Equals(query.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(user => user.IsActive);
        }
        else if (string.Equals(query.Status, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(user => !user.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role.Trim();
            users =
                from user in users
                join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
                join identityRole in dbContext.Roles on userRole.RoleId equals identityRole.Id
                where identityRole.Name == role
                select user;
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var descending = !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        users = query.SortBy.ToLowerInvariant() switch
        {
            "name" => descending
                ? users.OrderByDescending(user => user.FirstName).ThenByDescending(user => user.LastName)
                : users.OrderBy(user => user.FirstName).ThenBy(user => user.LastName),
            "email" => descending
                ? users.OrderByDescending(user => user.Email)
                : users.OrderBy(user => user.Email),
            "status" => descending
                ? users.OrderByDescending(user => user.IsActive)
                : users.OrderBy(user => user.IsActive),
            _ => descending
                ? users.OrderByDescending(user => user.CreatedAt)
                : users.OrderBy(user => user.CreatedAt)
        };

        var pageUsers = await users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.IsActive,
                user.EmailConfirmed,
                user.PhoneNumberConfirmed,
                user.CreatedAt,
                user.ProfileImageUrl,
                HasHealthProfile = dbContext.UserHealthProfiles.Any(profile => profile.UserId == user.Id)
            })
            .ToListAsync(cancellationToken);

        var userIds = pageUsers.Select(user => user.Id).ToList();
        var roleRows = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, Role = role.Name! })
            .ToListAsync(cancellationToken);

        var roles = roleRows
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Any(item => item.Role == "Admin") ? "Admin" : group.First().Role);

        var items = pageUsers
            .Select(user => new AdminUserListItemDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email ?? string.Empty,
                user.PhoneNumber,
                roles.GetValueOrDefault(user.Id, string.Empty),
                user.IsActive,
                user.EmailConfirmed,
                user.PhoneNumberConfirmed,
                user.CreatedAt,
                user.ProfileImageUrl,
                user.HasHealthProfile))
            .ToList();

        return new PagedResult<AdminUserListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task SetUserActiveStatusAsync(
        Guid actorUserId,
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == userId && !isActive)
        {
            throw new BadRequestException("Administrators cannot deactivate their own account.");
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == userId && !candidate.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("User", userId);

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!isActive)
        {
            var activeRefreshTokens = await dbContext.RefreshTokens
                .Where(token => token.UserId == userId && !token.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var refreshToken in activeRefreshTokens)
            {
                refreshToken.Revoke();
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var actor = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == actorUserId)
            .Select(u => new { u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        var actorName  = actor is null ? "Admin" : $"{actor.FirstName} {actor.LastName}".Trim();
        var actorEmail = actor?.Email ?? string.Empty;
        var action     = isActive ? "UserActivated" : "UserSuspended";
        var severity   = isActive ? "Success" : "Warning";
        var targetName = $"{user.FirstName} {user.LastName}".Trim();

        await auditLog.LogAsync(
            actorId:     actorUserId,
            actorName:   actorName,
            actorEmail:  actorEmail,
            module:      "Users",
            action:      action,
            target:      targetName,
            severity:    severity,
            description: isActive
                ? $"User account '{targetName}' was activated."
                : $"User account '{targetName}' was suspended.",
            changes:     [("Status", isActive ? "Inactive" : "Active", isActive ? "Active" : "Suspended")],
            cancellationToken: cancellationToken);
    }

    private static IReadOnlyList<AdminTrendPointDto> BuildSixMonthTrend(
        DateTime currentMonth,
        IReadOnlyDictionary<(int Year, int Month), int> values)
    {
        return Enumerable.Range(0, 6)
            .Select(offset => currentMonth.AddMonths(offset - 5))
            .Select(month => new AdminTrendPointDto(
                month.ToString("MMM"),
                values.GetValueOrDefault((month.Year, month.Month))))
            .ToList();
    }
}
