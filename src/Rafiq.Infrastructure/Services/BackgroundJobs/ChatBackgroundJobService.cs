using Hangfire;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services.BackgroundJobs;

public sealed class ChatBackgroundJobService(IBackgroundJobClient backgroundJobClient) : IChatBackgroundJobService
{
    public void EnqueueProcessMessage(
        Guid userId,
        Guid conversationId,
        Guid assistantMessageId,
        string userText,
        string? base64Image,
        string? imageFormat,
        string language,
        int utcOffsetMinutes)
    {
        backgroundJobClient.Enqueue<ChatMessageProcessorJob>(
            job => job.ExecuteAsync(
                userId, conversationId, assistantMessageId,
                userText, base64Image, imageFormat,
                language, utcOffsetMinutes));
    }
}
