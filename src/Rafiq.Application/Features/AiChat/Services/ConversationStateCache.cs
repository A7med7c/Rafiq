using System.Collections.Concurrent;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Application.Features.AiChat.Services;

/// <summary>
/// In-process, in-memory conversation state. Holds the last resolved family profile per
/// conversation so the AI can answer follow-up questions ("and her last lab?") without
/// repeating context. Lost on server restart — no DB changes needed.
/// Replace the backing store with IDistributedCache for multi-instance deployments.
/// </summary>
public sealed class ConversationStateCache : IConversationStateCache
{
    private readonly ConcurrentDictionary<Guid, ConversationState> _states = new();

    public ConversationState? GetState(Guid conversationId) =>
        _states.TryGetValue(conversationId, out var state) ? state : null;

    public void SetState(Guid conversationId, Guid profileId, string displayName) =>
        _states[conversationId] = new ConversationState(profileId, displayName, DateTimeOffset.UtcNow);

    public void ClearState(Guid conversationId) =>
        _states.TryRemove(conversationId, out _);
}
