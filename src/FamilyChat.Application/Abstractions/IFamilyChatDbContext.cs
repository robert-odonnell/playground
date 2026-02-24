using FamilyChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FamilyChat.Application.Abstractions;

public interface IFamilyChatDbContext
{
    DbSet<User> Users { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationMember> ConversationMembers { get; }
    DbSet<Message> Messages { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<ReadState> ReadStates { get; }
    DbSet<UserNotificationPreference> UserNotificationPreferences { get; }
    DbSet<ConversationNotificationPreference> ConversationNotificationPreferences { get; }
    DbSet<MagicLinkToken> MagicLinkTokens { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<DirectMessagePair> DirectMessagePairs { get; }
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
