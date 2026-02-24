using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Exceptions;
using FamilyChat.Contracts.Search;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Application.Services;

public sealed class SearchService(
    IFamilyChatDbContext dbContext,
    ICurrentUserAccessor currentUserAccessor)
{
    public async Task<SearchResponseDto> SearchAsync(
        string query,
        Guid? conversationId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResponseDto();
        }

        if (limit <= 0 || limit > 100)
        {
            limit = 50;
        }

        var userId = currentUserAccessor.UserId;

        var membershipQuery = dbContext.ConversationMembers
            .Where(member => member.UserId == userId);

        if (conversationId.HasValue)
        {
            var isMember = await dbContext.ConversationMembers.AnyAsync(
                member => member.ConversationId == conversationId.Value && member.UserId == userId,
                cancellationToken);

            if (!isMember)
            {
                throw new ForbiddenException("You are not a member of this conversation.");
            }

            membershipQuery = membershipQuery.Where(member => member.ConversationId == conversationId.Value);
        }

        var like = $"%{query.Trim()}%";

        var messageHits = await (
            from message in dbContext.Messages
            join member in membershipQuery on message.ConversationId equals member.ConversationId
            where message.DeletedAt == null &&
                  EF.Functions.Like(message.Body, like)
            select new SearchHitDto
            {
                Kind = SearchHitKind.Message,
                ConversationId = message.ConversationId,
                MessageId = message.Id,
                CreatedAt = message.CreatedAt,
                Snippet = message.Body.Length <= 160 ? message.Body : message.Body.Substring(0, 160)
            })
            .OrderByDescending(message => message.CreatedAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        var attachmentHits = await (
            from attachment in dbContext.Attachments
            join message in dbContext.Messages on attachment.MessageId equals message.Id
            join member in membershipQuery on message.ConversationId equals member.ConversationId
            where EF.Functions.Like(attachment.FileName, like)
            select new SearchHitDto
            {
                Kind = SearchHitKind.Attachment,
                ConversationId = message.ConversationId,
                MessageId = attachment.MessageId,
                CreatedAt = attachment.CreatedAt,
                FileName = attachment.FileName,
                ShareUrl = attachment.ShareUrl
            })
            .OrderByDescending(attachment => attachment.CreatedAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        return new SearchResponseDto
        {
            Hits = messageHits
                .Concat(attachmentHits)
                .OrderByDescending(hit => hit.CreatedAt)
                .Take(limit)
                .ToArray()
        };
    }
}
