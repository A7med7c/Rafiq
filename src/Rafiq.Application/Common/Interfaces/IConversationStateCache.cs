namespace Rafiq.Application.Common.Interfaces;

public interface IConversationStateCache
{
    ConversationState? GetState(Guid conversationId);
    void SetState(Guid conversationId, Guid profileId, string displayName);
    void ClearState(Guid conversationId);
}

public sealed record ConversationState(Guid ProfileId, string DisplayName, DateTimeOffset LastUpdated);
