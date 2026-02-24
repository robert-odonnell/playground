using FamilyChat.Contracts.Admin;
using FamilyChat.Contracts.Common;
using FamilyChat.Contracts.Conversations;
using FamilyChat.Contracts.Enums;
using FamilyChat.Contracts.Messages;
using FamilyChat.Domain.Entities;
using FamilyChat.Application.Utils;

namespace FamilyChat.Application.Utils;

public static class EntityMapping
{
    public static ConversationDto ToDto(
        this Conversation conversation,
        IReadOnlyList<ConversationMemberDto> members,
        int unreadCount,
        DateTime? lastMessageAt)
    {
        return new ConversationDto
        {
            Id = conversation.Id,
            Type = (ConversationType)conversation.Type,
            Name = conversation.Name,
            Topic = conversation.Topic,
            IsPrivate = conversation.IsPrivate,
            CreatedByUserId = conversation.CreatedByUserId,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = lastMessageAt,
            UnreadCount = unreadCount,
            Members = members
        };
    }

    public static ConversationMemberDto ToDto(this ConversationMember member)
    {
        return new ConversationMemberDto
        {
            UserId = member.UserId,
            Email = member.User?.Email ?? string.Empty,
            DisplayName = member.User?.DisplayName ?? string.Empty,
            JoinedAt = member.JoinedAt
        };
    }

    public static MessageDto ToDto(this Message message, Guid currentUserId)
    {
        var reactions = MessageJson.ParseReactions(message.ReactionsJson);
        var summaries = reactions
            .Select(pair => new ReactionSummaryDto
            {
                Emoji = pair.Key,
                Count = pair.Value.Length,
                ReactedByMe = pair.Value.Contains(currentUserId)
            })
            .OrderBy(summary => summary.Emoji, StringComparer.Ordinal)
            .ToArray();

        var attachments = message.Attachments
            .Select(attachment => new AttachmentDto
            {
                Id = attachment.Id,
                MessageId = attachment.MessageId,
                Provider = (Contracts.Enums.AttachmentProvider)attachment.Provider,
                FileId = attachment.FileId,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                Size = attachment.SizeBytes,
                ShareUrl = attachment.ShareUrl,
                CreatedAt = attachment.CreatedAt
            })
            .ToArray();

        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderDisplayName = message.Sender?.DisplayName ?? string.Empty,
            Body = message.Body,
            CreatedAt = message.CreatedAt,
            EditedAt = message.EditedAt,
            DeletedAt = message.DeletedAt,
            MentionUserIds = MessageJson.ParseMentions(message.MentionUserIdsJson),
            Reactions = reactions,
            ReactionSummaries = summaries,
            Attachments = attachments
        };
    }

    public static UserProfileDto ToProfileDto(this User user)
    {
        return new UserProfileDto(user.Id, user.Email, user.DisplayName, user.IsAdmin, user.IsDisabled);
    }

    public static AdminUserDto ToAdminDto(this User user)
    {
        return new AdminUserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            IsAdmin = user.IsAdmin,
            IsDisabled = user.IsDisabled,
            CreatedAt = user.CreatedAt
        };
    }

    public static AdminChannelDto ToAdminChannelDto(this Conversation conversation)
    {
        return new AdminChannelDto
        {
            Id = conversation.Id,
            Name = conversation.Name ?? string.Empty,
            Topic = conversation.Topic,
            IsPrivate = conversation.IsPrivate,
            CreatedByUserId = conversation.CreatedByUserId,
            CreatedAt = conversation.CreatedAt
        };
    }
}
