using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.AiChat.Commands.ArchiveConversation;
using Rafiq.Application.Features.AiChat.Commands.CreateConversation;
using Rafiq.Application.Features.AiChat.Commands.ReactToMessage;
using Rafiq.Application.Features.AiChat.Commands.RenameConversation;
using Rafiq.Application.Features.AiChat.Commands.SendMessage;
using Rafiq.Application.Features.AiChat.Queries.GenerateHealthSummary;
using Rafiq.Application.Features.AiChat.Queries.GetConversationHistory;
using Rafiq.Application.Features.AiChat.Queries.GetConversations;
using Rafiq.Domain.Enums;

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
    /// Lists the current user's conversations, ordered by most recent activity,
    /// for rendering the conversation-history sidebar. Does not include message content.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetConversationsQuery(), cancellationToken);
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
    /// Renames a conversation.
    /// </summary>
    [HttpPatch("conversations/{conversationId:guid}")]
    public async Task<IActionResult> RenameConversation(
        Guid conversationId,
        [FromBody] RenameConversationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RenameConversationCommand(conversationId, request.Title), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Archives (soft-deletes) a conversation. It stops appearing in the conversation
    /// list/history and can no longer receive new messages.
    /// </summary>
    [HttpDelete("conversations/{conversationId:guid}")]
    public async Task<IActionResult> ArchiveConversation(Guid conversationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ArchiveConversationCommand(conversationId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Generates an AI health summary for a profile using all available health data.
    /// Returns HasData=false without calling the AI when there is insufficient data.
    /// </summary>
    [HttpGet("health-summary/{profileId:guid}")]
    public async Task<IActionResult> GetHealthSummary(Guid profileId, [FromQuery] string language = "en", CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GenerateHealthSummaryQuery(profileId, language), cancellationToken);
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

    /// <summary>
    /// Adds or removes a 👍/👎 reaction on an assistant message.
    /// Set Remove=true to toggle the reaction off.
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages/{messageId:guid}/react")]
    public async Task<IActionResult> ReactToMessage(
        Guid conversationId,
        Guid messageId,
        [FromBody] ReactToMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReactToMessageCommand(conversationId, messageId, request.ReactionType, request.Remove, request.Feedback),
            cancellationToken);
        return Ok(result);
    }
}

public sealed class CreateConversationRequest
{
    public Guid UserHealthProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public sealed class RenameConversationRequest
{
    public string Title { get; set; } = string.Empty;
}

public sealed class SendMessageRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Base64Image { get; set; }
    public string? ImageFormat { get; set; }
}

public sealed class ReactToMessageRequest
{
    public ReactionType ReactionType { get; set; }
    public bool Remove { get; set; }
    public string? Feedback { get; set; }
}
