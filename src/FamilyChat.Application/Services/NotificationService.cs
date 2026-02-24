using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Exceptions;
using FamilyChat.Contracts.Notifications;
using FamilyChat.Contracts.Realtime;
using FamilyChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Application.Services;

public sealed class NotificationService(
    IFamilyChatDbContext dbContext,
    ICurrentUserAccessor currentUserAccessor) 
{
    public async Task<UserNotificationPreferenceDto> GetUserPreferenceAsync(CancellationToken cancellationToken)
    {
        var preference = await dbContext.UserNotificationPreferences
            .FirstOrDefaultAsync(item => item.UserId == currentUserAccessor.UserId, cancellationToken);

        return new UserNotificationPreferenceDto
        {
            InAppToastsEnabled = preference?.InAppToastsEnabled ?? true
        };
    }

    public async Task<UserNotificationPreferenceDto> UpdateUserPreferenceAsync(
        UserNotificationPreferenceDto request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var preference = await dbContext.UserNotificationPreferences
            .FirstOrDefaultAsync(item => item.UserId == currentUserAccessor.UserId, cancellationToken);

        if (preference is null)
        {
            preference = new UserNotificationPreference
            {
                UserId = currentUserAccessor.UserId,
                InAppToastsEnabled = request.InAppToastsEnabled,
                UpdatedAt = now
            };
            await dbContext.UserNotificationPreferences.AddAsync(preference, cancellationToken);
        }
        else
        {
            preference.InAppToastsEnabled = request.InAppToastsEnabled;
            preference.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task<ConversationNotificationPreferenceDto> GetConversationPreferenceAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        await EnsureMembershipAsync(conversationId, cancellationToken);

        var preference = await dbContext.ConversationNotificationPreferences
            .FirstOrDefaultAsync(
                item => item.ConversationId == conversationId && item.UserId == currentUserAccessor.UserId,
                cancellationToken);

        return new ConversationNotificationPreferenceDto
        {
            IsMuted = preference?.IsMuted ?? false
        };
    }

    public async Task<ConversationNotificationPreferenceDto> UpdateConversationPreferenceAsync(
        Guid conversationId,
        ConversationNotificationPreferenceDto request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await EnsureMembershipAsync(conversationId, cancellationToken);

        var preference = await dbContext.ConversationNotificationPreferences
            .FirstOrDefaultAsync(
                item => item.ConversationId == conversationId && item.UserId == currentUserAccessor.UserId,
                cancellationToken);

        if (preference is null)
        {
            preference = new ConversationNotificationPreference
            {
                ConversationId = conversationId,
                UserId = currentUserAccessor.UserId,
                IsMuted = request.IsMuted,
                UpdatedAt = now
            };
            await dbContext.ConversationNotificationPreferences.AddAsync(preference, cancellationToken);
        }
        else
        {
            preference.IsMuted = request.IsMuted;
            preference.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task UpdateReadStateAsync(
        Guid conversationId,
        DateTime requestedReadAt,
        CancellationToken cancellationToken)
    {
        await EnsureMembershipAsync(conversationId, cancellationToken);

        var latestMessageAt = await dbContext.Messages
            .Where(message => message.ConversationId == conversationId)
            .MaxAsync(message => (DateTime?)message.CreatedAt, cancellationToken) ?? DateTime.UtcNow;

        var clamped = requestedReadAt.Kind == DateTimeKind.Utc
            ? requestedReadAt
            : requestedReadAt.ToUniversalTime();

        if (clamped > latestMessageAt)
        {
            clamped = latestMessageAt;
        }

        var state = await dbContext.ReadStates
            .FirstOrDefaultAsync(
                item => item.ConversationId == conversationId && item.UserId == currentUserAccessor.UserId,
                cancellationToken);

        if (state is null)
        {
            state = new ReadState
            {
                ConversationId = conversationId,
                UserId = currentUserAccessor.UserId,
                LastReadAt = clamped
            };
            await dbContext.ReadStates.AddAsync(state, cancellationToken);
        }
        else if (state.LastReadAt < clamped)
        {
            state.LastReadAt = clamped;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UnreadUpdatedDto> BuildUnreadPayloadForUserAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var unreadCount = await CalculateUnreadForConversationAsync(userId, conversationId, cancellationToken);
        var totalUnreadConversations = await CalculateTotalUnreadConversationsAsync(userId, cancellationToken);

        return new UnreadUpdatedDto
        {
            ConversationId = conversationId,
            UnreadCount = unreadCount,
            TotalUnreadConversations = totalUnreadConversations
        };
    }

    public async Task<int> CalculateUnreadForConversationAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var lastReadAt = await dbContext.ReadStates
            .Where(state => state.UserId == userId && state.ConversationId == conversationId)
            .Select(state => (DateTime?)state.LastReadAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? DateTime.MinValue;

        return await dbContext.Messages
            .Where(message =>
                message.ConversationId == conversationId &&
                message.DeletedAt == null &&
                message.SenderId != userId &&
                message.CreatedAt > lastReadAt)
            .CountAsync(cancellationToken);
    }

    public async Task<int> CalculateTotalUnreadConversationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var conversationIds = await dbContext.ConversationMembers
            .Where(member => member.UserId == userId)
            .Select(member => member.ConversationId)
            .ToArrayAsync(cancellationToken);

        var total = 0;
        foreach (var conversationId in conversationIds)
        {
            if (await CalculateUnreadForConversationAsync(userId, conversationId, cancellationToken) > 0)
            {
                total++;
            }
        }

        return total;
    }

    private async Task EnsureMembershipAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var isMember = await dbContext.ConversationMembers.AnyAsync(
            member => member.ConversationId == conversationId && member.UserId == currentUserAccessor.UserId,
            cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException("You are not a member of this conversation.");
        }
    }
}
