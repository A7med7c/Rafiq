using Rafiq.Domain.Enums;

namespace Rafiq.Application.Common.Interfaces;

public interface IMessageReactionRepository
{
    Task UpsertAsync(Guid messageId, Guid userId, ReactionType reactionType, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid messageId, Guid userId, ReactionType reactionType, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, ReactionType>> GetUserReactionsForMessagesAsync(
        IReadOnlyList<Guid> messageIds, Guid userId, CancellationToken cancellationToken = default);
}
