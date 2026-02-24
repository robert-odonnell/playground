using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Exceptions;
using FamilyChat.Application.Utils;
using FamilyChat.Contracts.Admin;
using FamilyChat.Contracts.Conversations;
using FamilyChat.Domain.Entities;
using FamilyChat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Application.Services;

public sealed class AdminService(
    IFamilyChatDbContext dbContext,
    ICurrentUserAccessor currentUserAccessor,
    IRealtimePublisher realtimePublisher)
{
    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(cancellationToken);

        var users = await dbContext.Users
            .OrderBy(user => user.DisplayName)
            .ToArrayAsync(cancellationToken);

        return users.Select(user => user.ToAdminDto()).ToArray();
    }

    public async Task<AdminUserDto> CreateUserAsync(
        AdminCreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ValidationException("Email and display name are required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var existing = await dbContext.Users
            .AnyAsync(user => user.Email.ToLower() == email, cancellationToken);

        if (existing)
        {
            throw new ValidationException("A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            IsAdmin = request.IsAdmin,
            IsDisabled = false,
            CreatedAt = DateTime.UtcNow
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return user.ToAdminDto();
    }

    public async Task<AdminUserDto> UpdateUserAsync(
        Guid userId,
        AdminUpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(cancellationToken);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (request.DisplayName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                throw new ValidationException("Display name cannot be empty.");
            }

            user.DisplayName = request.DisplayName.Trim();
        }

        if (request.IsAdmin.HasValue)
        {
            user.IsAdmin = request.IsAdmin.Value;
        }

        if (request.IsDisabled.HasValue)
        {
            user.IsDisabled = request.IsDisabled.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return user.ToAdminDto();
    }

    public async Task<IReadOnlyList<AdminChannelDto>> GetChannelsAsync(CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(cancellationToken);

        var channels = await dbContext.Conversations
            .Where(conversation => conversation.Type == ConversationType.Channel)
            .OrderBy(conversation => conversation.Name)
            .ToArrayAsync(cancellationToken);

        return channels.Select(channel => channel.ToAdminChannelDto()).ToArray();
    }

    public async Task<AdminChannelDto> CreateChannelAsync(
        CreateChannelRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Channel name is required.");
        }

        var now = DateTime.UtcNow;

        var channel = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = ConversationType.Channel,
            Name = request.Name.Trim(),
            Topic = request.Topic?.Trim(),
            IsPrivate = request.IsPrivate,
            CreatedAt = now,
            CreatedByUserId = currentUserAccessor.UserId
        };

        await dbContext.Conversations.AddAsync(channel, cancellationToken);
        await dbContext.ConversationMembers.AddAsync(new ConversationMember
        {
            ConversationId = channel.Id,
            UserId = currentUserAccessor.UserId,
            JoinedAt = now
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = channel.ToAdminChannelDto();
        await realtimePublisher.PublishConversationUpdatedAsync(channel.Id, new Contracts.Conversations.ConversationDto
        {
            Id = channel.Id,
            Name = channel.Name,
            Topic = channel.Topic,
            IsPrivate = channel.IsPrivate,
            Type = Contracts.Enums.ConversationType.Channel,
            CreatedAt = channel.CreatedAt,
            CreatedByUserId = channel.CreatedByUserId,
            Members = [],
            UnreadCount = 0
        }, cancellationToken);

        return dto;
    }

    public async Task<AdminChannelDto> UpdateChannelAsync(
        Guid channelId,
        UpdateConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(cancellationToken);

        var channel = await dbContext.Conversations
            .FirstOrDefaultAsync(conversation =>
                conversation.Id == channelId && conversation.Type == ConversationType.Channel,
                cancellationToken)
            ?? throw new NotFoundException("Channel not found.");

        if (request.Name is not null)
        {
            channel.Name = string.IsNullOrWhiteSpace(request.Name)
                ? throw new ValidationException("Channel name cannot be empty.")
                : request.Name.Trim();
        }

        if (request.Topic is not null)
        {
            channel.Topic = request.Topic.Trim();
        }

        if (request.IsPrivate.HasValue)
        {
            channel.IsPrivate = request.IsPrivate.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return channel.ToAdminChannelDto();
    }

    public async Task AddChannelMemberAsync(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(cancellationToken);

        var channel = await dbContext.Conversations
            .FirstOrDefaultAsync(conversation =>
                conversation.Id == channelId && conversation.Type == ConversationType.Channel,
                cancellationToken)
            ?? throw new NotFoundException("Channel not found.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId && !candidate.IsDisabled, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var exists = await dbContext.ConversationMembers
            .AnyAsync(member => member.ConversationId == channel.Id && member.UserId == user.Id, cancellationToken);

        if (!exists)
        {
            var member = new ConversationMember
            {
                ConversationId = channel.Id,
                UserId = user.Id,
                JoinedAt = DateTime.UtcNow,
                User = user
            };

            await dbContext.ConversationMembers.AddAsync(member, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await realtimePublisher.PublishMemberJoinedAsync(channel.Id, member.ToDto(), cancellationToken);
        }
    }

    public async Task RemoveChannelMemberAsync(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(cancellationToken);

        var member = await dbContext.ConversationMembers
            .FirstOrDefaultAsync(item => item.ConversationId == channelId && item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Membership not found.");

        dbContext.ConversationMembers.Remove(member);
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimePublisher.PublishMemberLeftAsync(channelId, userId, cancellationToken);
    }

    private async Task EnsureAdminAsync(CancellationToken cancellationToken)
    {
        if (currentUserAccessor.IsAdmin)
        {
            return;
        }

        var isAdmin = await dbContext.Users
            .Where(user => user.Id == currentUserAccessor.UserId)
            .Select(user => user.IsAdmin)
            .FirstOrDefaultAsync(cancellationToken);

        if (!isAdmin)
        {
            throw new ForbiddenException("Admin access required.");
        }
    }
}
