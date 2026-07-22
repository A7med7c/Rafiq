using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.VoiceAgent.Commands.CreateVoiceSession;
using Rafiq.Application.Features.VoiceAgent.Commands.ProcessVoiceMessage;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/voice-agent")]
public class VoiceAgentController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new voice agent session tied to a health profile.
    /// Returns the session ID to use in subsequent message calls.
    /// </summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateVoiceSessionCommand(request.ProfileId, request.Language ?? "en"),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Synchronous mode — waits for the full agent loop and returns the response in the HTTP body.
    /// Use this for Swagger, Postman, automated tests, mobile clients, and third-party integrations.
    /// Also emits SignalR events (VoiceAgentThinking / VoiceAgentResponse) if the client is connected.
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid sessionId,
        [FromBody] SendVoiceMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ProcessVoiceMessageCommand(sessionId, request.Text, request.Language ?? "en",
                UtcOffsetMinutes: request.UtcOffsetMinutes),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Streaming mode — returns 202 Accepted immediately and runs the agent loop in the background.
    /// The final response and progress events are delivered exclusively through SignalR:
    ///   "VoiceAgentThinking" { toolName }                         — before each tool call
    ///   "VoiceAgentResponse" { sessionId, text, navigateTo, needsMoreInfo } — final answer
    ///   "VoiceAgentError"    { message }                          — unrecoverable failure
    /// Used by the Angular voice interface.
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/messages/stream")]
    public IActionResult SendMessageStream(
        Guid sessionId,
        [FromBody] SendVoiceMessageRequest request,
        [FromServices] IServiceScopeFactory scopeFactory,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] INotificationService notificationService,
        [FromServices] ILogger<VoiceAgentController> logger)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        var userIdValue  = userId.Value;
        var userIdString = userIdValue.ToString();
        var language     = request.Language ?? "en";
        var utcOffset    = request.UtcOffsetMinutes;

        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            try
            {
                // Pass BackgroundUserId so the handler doesn't rely on ICurrentUserService /
                // HttpContext, which is unavailable once the HTTP response has been sent.
                await sender.Send(new ProcessVoiceMessageCommand(
                    sessionId, request.Text, language, userIdValue, utcOffset));
            }
            catch (OperationCanceledException)
            {
                // Client disconnected — discard silently.
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unhandled exception in background voice loop for session {SessionId}", sessionId);
                try
                {
                    await notificationService.SendVoiceAgentErrorAsync(
                        userIdString,
                        new() { Message = "Something went wrong. Please try again." });
                }
                catch
                {
                    // ignore — nothing more we can do
                }
            }
        });

        return Accepted(new { sessionId });
    }
}

// ── Inline request DTOs ────────────────────────────────────────────────────────

public sealed record CreateSessionRequest(Guid ProfileId, string? Language);

public sealed record SendVoiceMessageRequest(string Text, string? Language, int UtcOffsetMinutes = 0);
