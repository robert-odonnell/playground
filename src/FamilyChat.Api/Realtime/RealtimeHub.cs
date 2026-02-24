using System.IdentityModel.Tokens.Jwt;
using FamilyChat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FamilyChat.Api.Realtime;

[Authorize]
public sealed class RealtimeHub(ConversationService conversationService) : Hub
{
    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetCurrentUserId();
        var isMember = await conversationService.IsConversationMemberAsync(conversationId, userId, Context.ConnectionAborted);

        if (!isMember)
        {
            throw new HubException("Forbidden");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Conversation(conversationId));
    }

    public Task LeaveConversation(Guid conversationId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.Conversation(conversationId));
    }

    private Guid GetCurrentUserId()
    {
        var raw = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (raw is null || !Guid.TryParse(raw, out var userId))
        {
            throw new HubException("Unauthorized");
        }

        return userId;
    }
}

internal static class GroupNames
{
    public static string Conversation(Guid conversationId) => $"conversation:{conversationId:D}";
}
