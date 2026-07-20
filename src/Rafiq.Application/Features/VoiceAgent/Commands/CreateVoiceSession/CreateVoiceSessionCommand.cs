using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.VoiceAgent.Commands.CreateVoiceSession;

public sealed record CreateVoiceSessionCommand(
    Guid ProfileId,
    string Language = "en")
    : IRequest<ApiResponse<Guid>>;
