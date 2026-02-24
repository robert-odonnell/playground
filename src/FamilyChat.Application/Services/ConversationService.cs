using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Exceptions;
using FamilyChat.Application.Utils;
using FamilyChat.Contracts.Conversations;
using FamilyChat.Domain.Entities;
using FamilyChat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Application.Services;

public sealed class ConversationService(
    IFamilyChatDbContext dbContext,
    ICurrentUserAccessor currentUserAccessor,
    NotificationService notificationService,
    IRealtimePublisher realtimePublisher)
{
    public async Task<IReadOnlyList<ConversationDto>> GetMyConversationsAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.UserId;
        var conversations = await dbContext.Conversations
            .Where(conversation => dbContext.ConversationMembers.Any(member =>
                member.ConversationId == conversation.Id && member.UserId == userId))
            .Include(conversation => conversation.Members)
            .ThenInclude(member => member.User)
            .ToArrayAsync(cancellationToken);

        var conversationIds = conversations.Select(conversation => conversation.Id).ToArray();

        var lastMessageByConversation = await dbContext.Messages
            .Where(message => conversationIds.Contains(message.ConversationId) && message.DeletedAt == null)
            .GroupBy(message => message.ConversationId)
            .Select(group => new { ConversationId = group.Key, LastMessageAt = group.Max(message => message.CreatedAt) })
            .ToDictionaryAsync(item => item.ConversationId, item => (DateTime?)item.LastMessageAt, cancellationToken);

        var response = new List<ConversationDto>(conversations.Length);

        foreach (var conversation in conversations)
        {
            var members = conversation.Members
                .OrderBy(member => member.JoinedAt)
                .Select(member => member.ToDto())
                .ToArray();

            var unread = await notificationService.CalculateUnreadForConversationAsync(
                userId,
                conversation.Id,
                cancellationToken);

            response.Add(conversation.ToDto(
                members,
                unread,
                lastMessageByConversation.GetValueOrDefault(conversation.Id)));
        }

        return response
            .OrderByDescending(conversation => conversation.LastMessageAt ?? conversation.CreatedAt)
            .ToArray();
    }

    public async Task<ConversationDto> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await LoadConversationForMemberAsync(conversationId, currentUserAccessor.UserId, cancellationToken);
        return await BuildConversationDtoAsync(conversation, currentUserAccessor.UserId, cancellationToken);
    }

    public async Task<ConversationDto> CreateChannelAsync(
        CreateChannelRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Channel name is required.");
        }

        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = ConversationType.Channel,
            Name = request.Name.Trim(),
            Topic = request.Topic?.Trim(),
            IsPrivate = request.IsPrivate,
            CreatedByUserId = currentUserAccessor.UserId,
            CreatedAt = now
        };

        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.ConversationMembers.AddAsync(new ConversationMember
        {
            ConversationId = conversation.Id,
            UserId = currentUserAccessor.UserId,
            JoinedAt = now
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await BuildConversationDtoAsync(conversation, currentUserAccessor.UserId, cancellationToken);
        await realtimePublisher.PublishConversationUpdatedAsync(conversation.Id, dto, cancellationToken);
        return dto;
    }

    public async Task<ConversationDto> CreateOrGetDmAsync(
        CreateDmRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.OtherUserId == Guid.Empty || request.OtherUserId == currentUserAccessor.UserId)
        {
            throw new ValidationException("A DM requires another valid user.");
        }

        var otherUser = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == request.OtherUserId && !user.IsDisabled, cancellationToken)
            ?? throw new NotFoundException("Target user not found.");

        var userA = currentUserAccessor.UserId;
        var userB = otherUser.Id;
        if (userA.CompareTo(userB) > 0)
        {
            (userA, userB) = (userB, userA);
        }

        var existingPair = await dbContext.DirectMessagePairs
            .FirstOrDefaultAsync(pair => pair.UserAId == userA && pair.UserBId == userB, cancellationToken);

        if (existingPair is not null)
        {
            var existingConversation = await LoadConversationForMemberAsync(
                existingPair.ConversationId,
                currentUserAccessor.UserId,
                cancellationToken);

            return await BuildConversationDtoAsync(existingConversation, currentUserAccessor.UserId, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = ConversationType.DM,
            Name = null,
            Topic = null,
            IsPrivate = true,
            CreatedByUserId = currentUserAccessor.UserId,
            CreatedAt = now
        };

        await dbContext.Conversations.AddAsync(conversation, cancellationToken);

        var members = new[]
        {
            new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = currentUserAccessor.UserId,
                JoinedAt = now
            },
            new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = otherUser.Id,
                JoinedAt = now
            }
        };

        await dbContext.ConversationMembers.AddRangeAsync(members, cancellationToken);
        await dbContext.DirectMessagePairs.AddAsync(new DirectMessagePair
        {
            UserAId = userA,
            UserBId = userB,
            ConversationId = conversation.Id
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await BuildConversationDtoAsync(conversation, currentUserAccessor.UserId, cancellationToken);
        await realtimePublisher.PublishConversationUpdatedAsync(conversation.Id, dto, cancellationToken);
        return dto;
    }

    public async Task<ConversationDto> CreateGroupDmAsync(
        CreateGroupDmRequestDto request,
        CancellationToken cancellationToken)
    {
        var userIds = request.UserIds
            .Append(currentUserAccessor.UserId)
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (userIds.Length < 3)
        {
            throw new ValidationException("A group DM requires at least 3 participants.");
        }

        var availableUserIds = await dbContext.Users
            .Where(user => userIds.Contains(user.Id) && !user.IsDisabled)
            .Select(user => user.Id)
            .ToArrayAsync(cancellationToken);

        if (availableUserIds.Length != userIds.Length)
        {
            throw new ValidationException("One or more selected users are invalid.");
        }

        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = ConversationType.GroupDM,
            Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            Topic = null,
            IsPrivate = true,
            CreatedByUserId = currentUserAccessor.UserId,
            CreatedAt = now
        };

        await dbContext.Conversations.AddAsync(conversation, cancellationToken);

        var members = userIds
            .Select(userId => new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = userId,
                JoinedAt = now
            })
            .ToArray();

        await dbContext.ConversationMembers.AddRangeAsync(members, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await BuildConversationDtoAsync(conversation, currentUserAccessor.UserId, cancellationToken);
        await realtimePublisher.PublishConversationUpdatedAsync(conversation.Id, dto, cancellationToken);
        return dto;
    }

    public async Task<ConversationDto> UpdateConversationAsync(
        Guid conversationId,
        UpdateConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        var conversation = await LoadConversationForMemberAsync(conversationId, currentUserAccessor.UserId, cancellationToken);

        if (conversation.Type != ConversationType.Channel)
        {
            throw new ValidationException("Only channels can be updated.");
        }

        await EnsureChannelManagerAsync(conversation, cancellationToken);

        if (request.Name is not null)
        {
            conversation.Name = string.IsNullOrWhiteSpace(request.Name)
                ? throw new ValidationException("Channel name cannot be empty.")
                : request.Name.Trim();
        }

        if (request.Topic is not null)
        {
            conversation.Topic = request.Topic.Trim();
        }

        if (request.IsPrivate.HasValue)
        {
            conversation.IsPrivate = request.IsPrivate.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await BuildConversationDtoAsync(conversation, currentUserAccessor.UserId, cancellationToken);
        await realtimePublisher.PublishConversationUpdatedAsync(conversation.Id, dto, cancellationToken);
        return dto;
    }

    public async Task<ConversationMemberDto> AddMemberAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ValidationException("User id is required.");
        }

        var conversation = await LoadConversationForMemberAsync(conversationId, currentUserAccessor.UserId, cancellationToken);

        if (conversation.Type != ConversationType.Channel)
        {
            throw new ValidationException("Members can be managed only for channels.");
        }

        await EnsureChannelManagerAsync(conversation, cancellationToken);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId && !candidate.IsDisabled, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var existing = await dbContext.ConversationMembers
            .Include(member => member.User)
            .FirstOrDefaultAsync(member => member.ConversationId == conversationId && member.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            return existing.ToDto();
        }

        var member = new ConversationMember
        {
            ConversationId = conversationId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            User = user
        };

        await dbContext.ConversationMembers.AddAsync(member, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = member.ToDto();
        await realtimePublisher.PublishMemberJoinedAsync(conversationId, dto, cancellationToken);
        return dto;
    }

    public async Task RemoveMemberAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        var conversation = await LoadConversationForMemberAsync(conversationId, currentUserAccessor.UserId, cancellationToken);

        if (conversation.Type == ConversationType.DM)
        {
            throw new ValidationException("DM membership is immutable.");
        }

        if (conversation.Type == ConversationType.Channel)
        {
            await EnsureChannelManagerAsync(conversation, cancellationToken);
        }
        else if (conversation.Type == ConversationType.GroupDM && userId != currentUserAccessor.UserId)
        {
            throw new ForbiddenException("Only self-removal is allowed for group DMs.");
        }

        var membership = await dbContext.ConversationMembers
            .FirstOrDefaultAsync(member => member.ConversationId == conversationId && member.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        dbContext.ConversationMembers.Remove(membership);

        var readState = await dbContext.ReadStates
            .FirstOrDefaultAsync(item => item.ConversationId == conversationId && item.UserId == userId, cancellationToken);
        if (readState is not null)
        {
            dbContext.ReadStates.Remove(readState);
        }

        var preference = await dbContext.ConversationNotificationPreferences
            .FirstOrDefaultAsync(item => item.ConversationId == conversationId && item.UserId == userId, cancellationToken);
        if (preference is not null)
        {
            dbContext.ConversationNotificationPreferences.Remove(preference);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimePublisher.PublishMemberLeftAsync(conversationId, userId, cancellationToken);
    }

    public async Task<ConversationDto> JoinConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .Include(item => item.Members)
            .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation not found.");

        if (conversation.Type != ConversationType.Channel || conversation.IsPrivate)
        {
            throw new ForbiddenException("Only public channels can be joined directly.");
        }

        var alreadyMember = conversation.Members.Any(member => member.UserId == currentUserAccessor.UserId);
        if (!alreadyMember)
        {
            var member = new ConversationMember
            {
                ConversationId = conversationId,
                UserId = currentUserAccessor.UserId,
                JoinedAt = DateTime.UtcNow
            };

            await dbContext.ConversationMembers.AddAsync(member, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var memberWithUser = await dbContext.ConversationMembers
                .Include(item => item.User)
                .FirstAsync(item => item.ConversationId == conversationId && item.UserId == currentUserAccessor.UserId, cancellationToken);

            await realtimePublisher.PublishMemberJoinedAsync(conversationId, memberWithUser.ToDto(), cancellationToken);
        }

        var reloaded = await LoadConversationForMemberAsync(conversationId, currentUserAccessor.UserId, cancellationToken);
        return await BuildConversationDtoAsync(reloaded, currentUserAccessor.UserId, cancellationToken);
    }

    public async Task LeaveConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await LoadConversationForMemberAsync(conversationId, currentUserAccessor.UserId, cancellationToken);

        if (conversation.Type == ConversationType.DM)
        {
            throw new ValidationException("Two-person DMs cannot be left.");
        }

        await RemoveMemberAsync(conversationId, currentUserAccessor.UserId, cancellationToken);
    }

    public async Task<bool> IsConversationMemberAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.ConversationMembers
            .AnyAsync(member => member.ConversationId == conversationId && member.UserId == userId, cancellationToken);
    }

    private async Task<Conversation> LoadConversationForMemberAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .Include(item => item.Members)
            .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation not found.");

        if (!conversation.Members.Any(member => member.UserId == userId))
        {
            throw new ForbiddenException("You are not a member of this conversation.");
        }

        return conversation;
    }

    private async Task<ConversationDto> BuildConversationDtoAsync(
        Conversation conversation,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var members = conversation.Members
            .OrderBy(member => member.JoinedAt)
            .Select(member => member.ToDto())
            .ToArray();

        var unread = await notificationService.CalculateUnreadForConversationAsync(userId, conversation.Id, cancellationToken);
        var lastMessageAt = await dbContext.Messages
            .Where(message => message.ConversationId == conversation.Id && message.DeletedAt == null)
            .MaxAsync(message => (DateTime?)message.CreatedAt, cancellationToken);

        return conversation.ToDto(members, unread, lastMessageAt);
    }

    private async Task EnsureChannelManagerAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        if (currentUserAccessor.IsAdmin)
        {
            return;
        }

        if (conversation.CreatedByUserId == currentUserAccessor.UserId)
        {
            return;
        }

        var isAdmin = await dbContext.Users
            .Where(user => user.Id == currentUserAccessor.UserId)
            .Select(user => user.IsAdmin)
            .FirstOrDefaultAsync(cancellationToken);

        if (!isAdmin)
        {
            throw new ForbiddenException("Only admins or channel creator can manage channel members.");
        }
    }
}
