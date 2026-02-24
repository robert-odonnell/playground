using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Exceptions;
using FamilyChat.Application.Utils;
using FamilyChat.Contracts.Messages;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Application.Services;

public sealed class ReactionService(
    IFamilyChatDbContext dbContext,
    ICurrentUserAccessor currentUserAccessor,
    IRealtimePublisher realtimePublisher)
{
    public async Task<MessageDto> ToggleReactionAsync(
        string messageId,
        string emoji,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(emoji))
        {
            throw new ValidationException("Emoji is required.");
        }

        emoji = Uri.UnescapeDataString(emoji);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var message = await dbContext.Messages
                .Include(item => item.Sender)
                .Include(item => item.Attachments)
                .FirstOrDefaultAsync(item => item.Id == messageId, cancellationToken)
                ?? throw new NotFoundException("Message not found.");

            var isMember = await dbContext.ConversationMembers.AnyAsync(
                member =>
                    member.ConversationId == message.ConversationId &&
                    member.UserId == currentUserAccessor.UserId,
                cancellationToken);

            if (!isMember)
            {
                throw new ForbiddenException("You are not a member of this conversation.");
            }

            var reactions = MessageJson.ParseReactions(message.ReactionsJson)
                .ToDictionary(
                    pair => pair.Key,
                    pair => new HashSet<Guid>(pair.Value));

            if (!reactions.TryGetValue(emoji, out var users))
            {
                users = new HashSet<Guid>();
                reactions[emoji] = users;
            }

            if (!users.Add(currentUserAccessor.UserId))
            {
                users.Remove(currentUserAccessor.UserId);
            }

            if (users.Count == 0)
            {
                reactions.Remove(emoji);
            }

            var serialized = reactions.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.OrderBy(id => id).ToArray());

            message.ReactionsJson = MessageJson.SerializeReactions(serialized);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);

                var dto = message.ToDto(currentUserAccessor.UserId);
                await realtimePublisher.PublishReactionUpdatedAsync(message.ConversationId, dto, cancellationToken);
                return dto;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < 3)
            {
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(cancellationToken);
                }
            }
        }

        throw new ValidationException("Could not update reaction due to concurrent writes. Retry.");
    }
}
