using FamilyChat.Application.Services;
using FamilyChat.Contracts.Common;
using FamilyChat.Contracts.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyChat.Api.Controllers;

[ApiController]
[Authorize]
[Route("conversations")]
public sealed class ConversationsController(ConversationService conversationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> GetMine(CancellationToken cancellationToken)
    {
        var response = await conversationService.GetMyConversationsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await conversationService.GetConversationAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPost("channel")]
    public async Task<ActionResult<ConversationDto>> CreateChannel(
        [FromBody] CreateChannelRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await conversationService.CreateChannelAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("dm")]
    public async Task<ActionResult<ConversationDto>> CreateDm(
        [FromBody] CreateDmRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await conversationService.CreateOrGetDmAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("groupdm")]
    public async Task<ActionResult<ConversationDto>> CreateGroupDm(
        [FromBody] CreateGroupDmRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await conversationService.CreateGroupDmAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ConversationDto>> UpdateConversation(
        Guid id,
        [FromBody] UpdateConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await conversationService.UpdateConversationAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/join")]
    public async Task<ActionResult<ConversationDto>> Join(Guid id, CancellationToken cancellationToken)
    {
        var response = await conversationService.JoinConversationAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<ActionResult<ApiAckDto>> Leave(Guid id, CancellationToken cancellationToken)
    {
        await conversationService.LeaveConversationAsync(id, cancellationToken);
        return Ok(new ApiAckDto());
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<ConversationMemberDto>> AddMember(
        Guid id,
        [FromBody] AddMemberRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await conversationService.AddMemberAsync(id, request.UserId, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}/members/{uid:guid}")]
    public async Task<ActionResult<ApiAckDto>> RemoveMember(Guid id, Guid uid, CancellationToken cancellationToken)
    {
        await conversationService.RemoveMemberAsync(id, uid, cancellationToken);
        return Ok(new ApiAckDto());
    }
}
