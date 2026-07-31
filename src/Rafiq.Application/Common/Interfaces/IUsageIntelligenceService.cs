using Rafiq.Application.Features.Admin;

namespace Rafiq.Application.Common.Interfaces;

public interface IUsageIntelligenceService
{
    Task<UsageIntelligenceOverviewDto> GetOverviewAsync(CancellationToken ct = default);
    Task<PagedResult<UsageAttentionUserDto>> GetAttentionQueueAsync(UsageAttentionQueueQuery query, CancellationToken ct = default);
    Task<UsageUserDetailDto> GetUserDetailAsync(Guid userId, CancellationToken ct = default);
    Task TakeActionAsync(Guid targetUserId, Guid adminId, string adminName, TakeUsageActionDto dto, CancellationToken ct = default);
    Task SaveFlaggedRequestAsync(AiClassificationContext context, CancellationToken ct = default);
}

public sealed record AiClassificationContext(
    Guid   UserId,
    string RequestType,
    string UserRequest,
    string AiResponse,
    string Classification,
    string Reason);
