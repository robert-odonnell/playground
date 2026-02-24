using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Services;
using FamilyChat.Contracts.Common;
using FamilyChat.Contracts.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyChat.Api.Controllers;

[ApiController]
[Authorize]
[Route("notifications")]
public sealed class NotificationsController(
    NotificationService notificationService,
    ICurrentUserAccessor currentUserAccessor,
    IRealtimePublisher realtimePublisher) : ControllerBase
{
    [HttpGet("preferences")]
    public async Task<ActionResult<UserNotificationPreferenceDto>> GetUserPreference(CancellationToken cancellationToken)
    {
        var response = await notificationService.GetUserPreferenceAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<UserNotificationPreferenceDto>> UpdateUserPreference(
        [FromBody] UserNotificationPreferenceDto request,
        CancellationToken cancellationToken)
    {
        var response = await notificationService.UpdateUserPreferenceAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("/conversations/{id:guid}/notification")]
    public async Task<ActionResult<ConversationNotificationPreferenceDto>> GetConversationPreference(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await notificationService.GetConversationPreferenceAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPut("/conversations/{id:guid}/notification")]
    public async Task<ActionResult<ConversationNotificationPreferenceDto>> UpdateConversationPreference(
        Guid id,
        [FromBody] ConversationNotificationPreferenceDto request,
        CancellationToken cancellationToken)
    {
        var response = await notificationService.UpdateConversationPreferenceAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("/conversations/{id:guid}/read")]
    public async Task<ActionResult<ApiAckDto>> UpdateReadState(
        Guid id,
        [FromBody] UpdateReadStateRequestDto request,
        CancellationToken cancellationToken)
    {
        await notificationService.UpdateReadStateAsync(id, request.LastReadAt, cancellationToken);
        var payload = await notificationService.BuildUnreadPayloadForUserAsync(currentUserAccessor.UserId, id, cancellationToken);
        await realtimePublisher.PublishUnreadUpdatedAsync(currentUserAccessor.UserId, payload, cancellationToken);
        return Ok(new ApiAckDto());
    }
}
