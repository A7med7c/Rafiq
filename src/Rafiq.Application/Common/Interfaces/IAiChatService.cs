using Rafiq.Application.AI.Models;

namespace Rafiq.Application.Common.Interfaces;

public interface IAiChatService
{
    Task<AiChatResponse> GenerateResponseAsync(AiChatRequest request, CancellationToken cancellationToken = default);
}