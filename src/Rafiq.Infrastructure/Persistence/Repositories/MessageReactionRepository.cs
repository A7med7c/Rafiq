using Microsoft.EntityFrameworkCore;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Enums;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class MessageReactionRepository(RafiqDbContext context) : IMessageReactionRepository
{
    public async Task UpsertAsync(Guid messageId, Guid userId, ReactionType reactionType, string? feedback = null, CancellationToken cancellationToken = default)
    {
        // Remove any existing reaction of a DIFFERENT type (one reaction at a time per user)
        var others = await context.MessageReactions
            .Where(r => r.AiMessageId == messageId && r.UserId == userId && r.ReactionType != reactionType)
            .ToListAsync(cancellationToken);

        if (others.Count > 0)
            context.MessageReactions.RemoveRange(others);

        // Update feedback if reaction already exists; otherwise insert
        var existing = await context.MessageReactions.FirstOrDefaultAsync(
            r => r.AiMessageId == messageId && r.UserId == userId && r.ReactionType == reactionType,
            cancellationToken);

        if (existing is not null)
            existing.UpdateFeedback(feedback);
        else
            await context.MessageReactions.AddAsync(new MessageReaction(messageId, userId, reactionType, feedback), cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid messageId, Guid userId, ReactionType reactionType, CancellationToken cancellationToken = default)
    {
        var reaction = await context.MessageReactions.FirstOrDefaultAsync(
            r => r.AiMessageId == messageId && r.UserId == userId && r.ReactionType == reactionType,
            cancellationToken);

        if (reaction is not null)
        {
            context.MessageReactions.Remove(reaction);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Dictionary<Guid, ReactionType>> GetUserReactionsForMessagesAsync(
        IReadOnlyList<Guid> messageIds, Guid userId, CancellationToken cancellationToken = default)
    {
        if (messageIds.Count == 0)
            return new Dictionary<Guid, ReactionType>();

        return await context.MessageReactions
            .Where(r => messageIds.Contains(r.AiMessageId) && r.UserId == userId)
            .ToDictionaryAsync(r => r.AiMessageId, r => r.ReactionType, cancellationToken);
    }
}
