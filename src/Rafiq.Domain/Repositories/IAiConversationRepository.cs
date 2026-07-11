using Rafiq.Domain.Entities.Chat;

namespace Rafiq.Domain.Repositories;

public interface IAiConversationRepository
{
    Task<AiConversation?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<AiConversation?> GetWithMessagesAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiConversation>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(AiConversation conversation, CancellationToken cancellationToken = default);
    Task AddMessageAsync(AiMessage message, CancellationToken cancellationToken = default);
    void Remove(AiConversation conversation);
}
