using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Exceptions;
using FamilyChat.Application.Utils;
using FamilyChat.Contracts.Common;
using FamilyChat.Contracts.Messages;
using FamilyChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Application.Services;

public sealed class MessageService(
    IFamilyChatDbContext dbContext,
    ICurrentUserAccessor currentUserAccessor,
    IUlidGenerator ulidGenerator,
    NotificationService notificationService,
    IRealtimePublisher realtimePublisher)
{
    public async Task<PagedResultDto<MessageDto>> GetMessagesAsync(
        Guid conversationId,
        string? before,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit <= 0 || limit > 100)
        {
            limit = 50;
        }

        await EnsureMembershipAsync(conversationId, cancellationToken);

        var query = dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .Include(message => message.Sender)
            .Include(message => message.Attachments)
            .OrderByDescending(message => message.CreatedAt)
            .ThenByDescending(message => message.Id)
            .AsQueryable();

        var cursor = CursorCodec.Decode(before);
        if (cursor.HasValue)
        {
            var cursorCreatedAt = cursor.Value.CreatedAtUtc;
            var cursorMessageId = cursor.Value.MessageId;

            query = query.Where(message =>
                message.CreatedAt < cursorCreatedAt ||
                (message.CreatedAt == cursorCreatedAt && string.CompareOrdinal(message.Id, cursorMessageId) < 0));
        }

        var entities = await query.Take(limit).ToArrayAsync(cancellationToken);
        var ordered = entities
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id, StringComparer.Ordinal)
            .ToArray();

        string? nextCursor = null;
        if (entities.Length == limit)
        {
            var oldest = entities.Last();
            nextCursor = CursorCodec.Encode(oldest.CreatedAt, oldest.Id);
        }

        return new PagedResultDto<MessageDto>
        {
            Items = ordered.Select(message => message.ToDto(currentUserAccessor.UserId)).ToArray(),
            NextCursor = nextCursor
        };
    }

    public async Task<MessageDto> CreateMessageAsync(
        Guid conversationId,
        CreateMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation not found.");

        await EnsureMembershipAsync(conversationId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Body) && (request.Attachments is null || request.Attachments.Count == 0))
        {
            throw new ValidationException("Message body or attachments are required.");
        }

        var now = DateTime.UtcNow;
        var mentionIds = await ResolveMentionIdsAsync(conversationId, request.Body, cancellationToken);
        var entity = new Message
        {
            Id = ulidGenerator.NewUlid(),
            ConversationId = conversationId,
            SenderId = currentUserAccessor.UserId,
            Body = request.Body.Trim(),
            CreatedAt = now,
            MentionUserIdsJson = MessageJson.SerializeMentions(mentionIds),
            ReactionsJson = "{}"
        };

        await dbContext.Messages.AddAsync(entity, cancellationToken);

        if (request.Attachments is not null)
        {
            foreach (var attachment in request.Attachments)
            {
                ValidateAttachment(attachment);
                await dbContext.Attachments.AddAsync(new Attachment
                {
                    Id = Guid.NewGuid(),
                    MessageId = entity.Id,
                    Provider = (Domain.Enums.AttachmentProvider)attachment.Provider,
                    FileId = attachment.FileId,
                    FileName = attachment.FileName.Trim(),
                    ContentType = attachment.ContentType?.Trim(),
                    SizeBytes = attachment.Size,
                    ShareUrl = attachment.ShareUrl.Trim(),
                    CreatedAt = now
                }, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var message = await LoadMessageAsync(entity.Id, cancellationToken);
        var dto = message.ToDto(currentUserAccessor.UserId);

        await realtimePublisher.PublishMessageCreatedAsync(conversationId, dto, cancellationToken);

        var recipientIds = await dbContext.ConversationMembers
            .Where(member => member.ConversationId == conversationId && member.UserId != currentUserAccessor.UserId)
            .Select(member => member.UserId)
            .ToArrayAsync(cancellationToken);

        foreach (var recipientId in recipientIds)
        {
            var payload = await notificationService.BuildUnreadPayloadForUserAsync(recipientId, conversationId, cancellationToken);
            await realtimePublisher.PublishUnreadUpdatedAsync(recipientId, payload, cancellationToken);
        }

        return dto;
    }

    public async Task<MessageDto> UpdateMessageAsync(
        string messageId,
        UpdateMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ValidationException("Message body is required.");
        }

        var entity = await dbContext.Messages
            .FirstOrDefaultAsync(message => message.Id == messageId, cancellationToken)
            ?? throw new NotFoundException("Message not found.");

        await EnsureMembershipAsync(entity.ConversationId, cancellationToken);

        if (entity.SenderId != currentUserAccessor.UserId && !currentUserAccessor.IsAdmin)
        {
            throw new ForbiddenException("Only the sender or an admin can edit this message.");
        }

        if (entity.DeletedAt is not null)
        {
            throw new ValidationException("Deleted messages cannot be edited.");
        }

        entity.Body = request.Body.Trim();
        entity.EditedAt = DateTime.UtcNow;
        var mentionIds = await ResolveMentionIdsAsync(entity.ConversationId, request.Body, cancellationToken);
        entity.MentionUserIdsJson = MessageJson.SerializeMentions(mentionIds);

        await dbContext.SaveChangesAsync(cancellationToken);

        var message = await LoadMessageAsync(entity.Id, cancellationToken);
        var dto = message.ToDto(currentUserAccessor.UserId);
        await realtimePublisher.PublishMessageUpdatedAsync(entity.ConversationId, dto, cancellationToken);
        return dto;
    }

    public async Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Messages
            .FirstOrDefaultAsync(message => message.Id == messageId, cancellationToken)
            ?? throw new NotFoundException("Message not found.");

        await EnsureMembershipAsync(entity.ConversationId, cancellationToken);

        if (entity.SenderId != currentUserAccessor.UserId && !currentUserAccessor.IsAdmin)
        {
            throw new ForbiddenException("Only the sender or an admin can delete this message.");
        }

        if (entity.DeletedAt is not null)
        {
            return;
        }

        entity.DeletedAt = DateTime.UtcNow;
        entity.Body = string.Empty;
        entity.MentionUserIdsJson = "[]";

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimePublisher.PublishMessageDeletedAsync(entity.ConversationId, entity.Id, cancellationToken);
    }

    public async Task<MessageDto> GetMessageAsync(string messageId, CancellationToken cancellationToken)
    {
        var message = await LoadMessageAsync(messageId, cancellationToken);
        await EnsureMembershipAsync(message.ConversationId, cancellationToken);
        return message.ToDto(currentUserAccessor.UserId);
    }

    private static void ValidateAttachment(AttachmentInputDto attachment)
    {
        if (string.IsNullOrWhiteSpace(attachment.FileName))
        {
            throw new ValidationException("Attachment fileName is required.");
        }

        if (string.IsNullOrWhiteSpace(attachment.ShareUrl) ||
            !Uri.TryCreate(attachment.ShareUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ValidationException("Attachment shareUrl must be a valid absolute URL.");
        }
    }

    private async Task<IReadOnlyList<Guid>> ResolveMentionIdsAsync(
        Guid conversationId,
        string body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        var tokens = MessageJson.ExtractMentionTokens(body);
        if (tokens.Count == 0)
        {
            return [];
        }

        var users = await dbContext.ConversationMembers
            .Where(member => member.ConversationId == conversationId)
            .Select(member => member.User!)
            .Where(user => !user.IsDisabled)
            .ToArrayAsync(cancellationToken);

        var tokenSet = new HashSet<string>(tokens, StringComparer.OrdinalIgnoreCase);

        return users
            .Where(user =>
            {
                if (tokenSet.Contains(user.DisplayName))
                {
                    return true;
                }

                var localPart = user.Email.Split('@')[0];
                return tokenSet.Contains(localPart);
            })
            .Select(user => user.Id)
            .Distinct()
            .ToArray();
    }

    private async Task<Message> LoadMessageAsync(string messageId, CancellationToken cancellationToken)
    {
        return await dbContext.Messages
            .Include(message => message.Sender)
            .Include(message => message.Attachments)
            .FirstOrDefaultAsync(message => message.Id == messageId, cancellationToken)
            ?? throw new NotFoundException("Message not found.");
    }

    private async Task EnsureMembershipAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var isMember = await dbContext.ConversationMembers
            .AnyAsync(member =>
                member.ConversationId == conversationId && member.UserId == currentUserAccessor.UserId,
                cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException("You are not a member of this conversation.");
        }
    }
}
