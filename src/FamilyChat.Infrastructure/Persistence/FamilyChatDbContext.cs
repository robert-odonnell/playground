using FamilyChat.Application.Abstractions;
using FamilyChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyChat.Infrastructure.Persistence;

public sealed class FamilyChatDbContext(DbContextOptions<FamilyChatDbContext> options)
    : DbContext(options), IFamilyChatDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ReadState> ReadStates => Set<ReadState>();
    public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();
    public DbSet<ConversationNotificationPreference> ConversationNotificationPreferences => Set<ConversationNotificationPreference>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DirectMessagePair> DirectMessagePairs => Set<DirectMessagePair>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "dbo");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Email).HasMaxLength(256).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(128).IsRequired();
            entity.Property(item => item.IsAdmin).HasDefaultValue(false);
            entity.Property(item => item.IsDisabled).HasDefaultValue(false);
            entity.Property(item => item.CreatedAt).HasPrecision(3);
            entity.HasIndex(item => item.Email).IsUnique();
            entity.HasIndex(item => item.IsDisabled);
        });

        builder.Entity<Conversation>(entity =>
        {
            entity.ToTable("Conversations", "dbo");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Type).HasConversion<byte>();
            entity.Property(item => item.Name).HasMaxLength(128);
            entity.Property(item => item.Topic).HasMaxLength(512);
            entity.Property(item => item.IsPrivate).HasDefaultValue(false);
            entity.Property(item => item.CreatedAt).HasPrecision(3);
            entity.HasIndex(item => item.Type);
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ConversationMember>(entity =>
        {
            entity.ToTable("ConversationMembers", "dbo");
            entity.HasKey(item => new { item.ConversationId, item.UserId });
            entity.Property(item => item.JoinedAt).HasPrecision(3);
            entity.HasIndex(item => item.UserId);
            entity.HasOne(item => item.Conversation)
                .WithMany(conversation => conversation.Members)
                .HasForeignKey(item => item.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.User)
                .WithMany(user => user.ConversationMembers)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages", "dbo");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnType("char(26)");
            entity.Property(item => item.Body).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(item => item.CreatedAt).HasPrecision(3);
            entity.Property(item => item.EditedAt).HasPrecision(3);
            entity.Property(item => item.DeletedAt).HasPrecision(3);
            entity.Property(item => item.MentionUserIdsJson).HasColumnType("nvarchar(max)").HasDefaultValue("[]").IsRequired();
            entity.Property(item => item.ReactionsJson).HasColumnType("nvarchar(max)").HasDefaultValue("{}").IsRequired();
            entity.Property(item => item.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasIndex(item => new { item.ConversationId, item.CreatedAt, item.Id }).IsDescending(false, true, true);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Messages_MentionUserIdsJson_IsJson", "ISJSON([MentionUserIdsJson]) = 1");
                table.HasCheckConstraint("CK_Messages_ReactionsJson_IsJson", "ISJSON([ReactionsJson]) = 1");
            });
            entity.HasOne(item => item.Conversation)
                .WithMany(conversation => conversation.Messages)
                .HasForeignKey(item => item.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Sender)
                .WithMany(user => user.Messages)
                .HasForeignKey(item => item.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Attachment>(entity =>
        {
            entity.ToTable("Attachments", "dbo");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Provider).HasConversion<byte>();
            entity.Property(item => item.MessageId).HasColumnType("char(26)").IsRequired();
            entity.Property(item => item.FileId).HasMaxLength(256);
            entity.Property(item => item.FileName).HasMaxLength(512).IsRequired();
            entity.Property(item => item.ContentType).HasMaxLength(256);
            entity.Property(item => item.ShareUrl).HasMaxLength(2048).IsRequired();
            entity.Property(item => item.CreatedAt).HasPrecision(3);
            entity.HasIndex(item => item.MessageId);
            entity.HasIndex(item => item.FileName);
            entity.HasOne(item => item.Message)
                .WithMany(message => message.Attachments)
                .HasForeignKey(item => item.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReadState>(entity =>
        {
            entity.ToTable("ReadStates", "dbo");
            entity.HasKey(item => new { item.ConversationId, item.UserId });
            entity.Property(item => item.LastReadAt).HasPrecision(3);
            entity.HasIndex(item => item.UserId);
            entity.HasOne(item => item.Conversation)
                .WithMany()
                .HasForeignKey(item => item.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserNotificationPreference>(entity =>
        {
            entity.ToTable("UserNotificationPreferences", "dbo");
            entity.HasKey(item => item.UserId);
            entity.Property(item => item.InAppToastsEnabled).HasDefaultValue(true);
            entity.Property(item => item.UpdatedAt).HasPrecision(3);
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ConversationNotificationPreference>(entity =>
        {
            entity.ToTable("ConversationNotificationPreferences", "dbo");
            entity.HasKey(item => new { item.ConversationId, item.UserId });
            entity.Property(item => item.IsMuted).HasDefaultValue(false);
            entity.Property(item => item.UpdatedAt).HasPrecision(3);
            entity.HasOne(item => item.Conversation)
                .WithMany()
                .HasForeignKey(item => item.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MagicLinkToken>(entity =>
        {
            entity.ToTable("MagicLinkTokens", "dbo");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.CreatedAt).HasPrecision(3);
            entity.Property(item => item.ExpiresAt).HasPrecision(3);
            entity.Property(item => item.ConsumedAt).HasPrecision(3);
            entity.HasIndex(item => item.TokenHash);
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", "dbo");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.InstallationId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.CreatedAt).HasPrecision(3);
            entity.Property(item => item.ExpiresAt).HasPrecision(3);
            entity.Property(item => item.RevokedAt).HasPrecision(3);
            entity.HasIndex(item => item.TokenHash);
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DirectMessagePair>(entity =>
        {
            entity.ToTable("DirectMessagePairs", "dbo");
            entity.HasKey(item => new { item.UserAId, item.UserBId });
            entity.HasIndex(item => item.ConversationId).IsUnique();
            entity.HasOne(item => item.Conversation)
                .WithMany()
                .HasForeignKey(item => item.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
