using FamilyChat.Application.Services;
using FamilyChat.Contracts.Common;
using FamilyChat.Contracts.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyChat.Api.Controllers;

[ApiController]
[Authorize]
public sealed class MessagesController(MessageService messageService, ReactionService reactionService) : ControllerBase
{
    [HttpGet("conversations/{id:guid}/messages")]
    public async Task<ActionResult<PagedResultDto<MessageDto>>> GetMessages(
        Guid id,
        [FromQuery] string? before,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var response = await messageService.GetMessagesAsync(id, before, limit, cancellationToken);
        return Ok(response);
    }

    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<ActionResult<MessageDto>> CreateMessage(
        Guid id,
        [FromBody] CreateMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await messageService.CreateMessageAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("messages/{id}")]
    public async Task<ActionResult<MessageDto>> UpdateMessage(
        string id,
        [FromBody] UpdateMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await messageService.UpdateMessageAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("messages/{id}")]
    public async Task<ActionResult<ApiAckDto>> DeleteMessage(string id, CancellationToken cancellationToken)
    {
        await messageService.DeleteMessageAsync(id, cancellationToken);
        return Ok(new ApiAckDto());
    }

    [HttpPut("messages/{id}/reactions/{emoji}")]
    public async Task<ActionResult<MessageDto>> ToggleReaction(
        string id,
        string emoji,
        CancellationToken cancellationToken)
    {
        var response = await reactionService.ToggleReactionAsync(id, emoji, cancellationToken);
        return Ok(response);
    }
}
