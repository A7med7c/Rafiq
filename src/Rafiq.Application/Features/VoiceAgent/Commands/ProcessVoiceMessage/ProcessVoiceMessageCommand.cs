using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.VoiceAgent.DTOs;

namespace Rafiq.Application.Features.VoiceAgent.Commands.ProcessVoiceMessage;

public sealed record ProcessVoiceMessageCommand(
    Guid SessionId,
    string Text,
    string Language = "en",
    /// <summary>
    /// When running in a background task (fire-and-forget controller), pass the authenticated
    /// userId here so the handler doesn't rely on ICurrentUserService / HttpContext,
    /// which is unavailable after the HTTP response is sent.
    /// Leave as default (Guid.Empty) for synchronous calls — the handler reads from ICurrentUserService.
    /// </summary>
    Guid BackgroundUserId = default,
    /// <summary>Client's UTC offset in minutes (e.g. +180 for Egypt UTC+3).</summary>
    int UtcOffsetMinutes = 0)
    : IRequest<ApiResponse<VoiceAgentResponseDto>>, IAiCommand;
