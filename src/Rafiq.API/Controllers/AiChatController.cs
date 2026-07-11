using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.AiChat.Commands.CreateConversation;
using Rafiq.Application.Features.AiChat.Commands.SendMessage;
using Rafiq.Application.Features.AiChat.Queries.GetConversationHistory;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/chat")]
public class AiChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public AiChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new AI chat conversation for a health profile.
    /// </summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateConversationCommand(request.UserHealthProfileId, request.Title),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the full message history for a conversation.
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<IActionResult> GetHistory(Guid conversationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetConversationHistoryQuery(conversationId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Sends a message (text-only or with an image) to an existing conversation.
    /// Optionally include Base64Image and ImageFormat for multimodal (image+question) requests.
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SendMessageCommand(conversationId, request.Text, request.Base64Image, request.ImageFormat),
            cancellationToken);
        return Ok(result);
    }
}

public sealed class CreateConversationRequest
{
    public Guid UserHealthProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public sealed class SendMessageRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Base64Image { get; set; }
    public string? ImageFormat { get; set; }
}
