using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class AiConversationRepository(RafiqDbContext context) : IAiConversationRepository
{
    public Task<AiConversation?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return context.AiConversations
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }

    public Task<AiConversation?> GetWithMessagesAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return context.AiConversations
            .Include(x => x.Messages.OrderBy(m => m.SequenceNumber))
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(AiConversation conversation, CancellationToken cancellationToken = default)
    {
        await context.AiConversations.AddAsync(conversation, cancellationToken);
    }

    public async Task AddMessageAsync(AiMessage message, CancellationToken cancellationToken = default)
    {
        await context.AiMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<AiConversation>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.AiConversations
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Remove(AiConversation conversation)
    {
        context.AiConversations.Remove(conversation);
    }

    public Task HardDeleteAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => context.AiConversations
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
}
