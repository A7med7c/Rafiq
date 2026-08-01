using Rafiq.Application.Features.Admin;

namespace Rafiq.Application.Common.Interfaces;

public interface IAdminAiService
{
    Task<AiOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<AiRequestListItemDto>> GetRequestsAsync(
        AiRequestQuery query,
        CancellationToken cancellationToken = default);

    Task<AiRequestDetailDto> GetRequestDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AiFeedbackListItemDto>> GetFeedbackAsync(
        AiFeedbackQuery query,
        CancellationToken cancellationToken = default);

    Task UpdateFeedbackAsync(
        Guid id,
        UpdateAiFeedbackDto dto,
        CancellationToken cancellationToken = default);

    Task<AiPerformanceDto> GetPerformanceAsync(
        int days = 30,
        CancellationToken cancellationToken = default);

    Task<AiInsightsDto> GetInsightsAsync(CancellationToken cancellationToken = default);
}
