namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// Decouples the Application layer from the Hangfire infrastructure by hiding
/// the concrete job-enqueue call behind a simple interface.
/// </summary>
public interface IChatBackgroundJobService
{
    void EnqueueProcessMessage(
        Guid userId,
        Guid conversationId,
        Guid assistantMessageId,
        string userText,
        string? base64Image,
        string? imageFormat,
        string language,
        int utcOffsetMinutes);
}

/// <summary>
/// Queues an AI request classification job without coupling the Application layer to Hangfire.
/// </summary>
public interface IRequestClassificationJobService
{
    void EnqueueClassification(Guid userId, string requestType, string userRequest, string aiResponse);
}
