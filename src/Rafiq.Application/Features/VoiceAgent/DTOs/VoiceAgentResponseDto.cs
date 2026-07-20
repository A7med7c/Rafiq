namespace Rafiq.Application.Features.VoiceAgent.DTOs;

public sealed record VoiceAgentResponseDto(
    Guid SessionId,
    string Response,
    string? NavigateTo,
    bool NeedsMoreInfo);
