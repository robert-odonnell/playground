IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[Users] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [DisplayName] nvarchar(128) NOT NULL,
    [IsAdmin] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsDisabled] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[Conversations] (
    [Id] uniqueidentifier NOT NULL,
    [Type] tinyint NOT NULL,
    [Name] nvarchar(128) NULL,
    [Topic] nvarchar(512) NULL,
    [IsPrivate] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedByUserId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Conversations_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[MagicLinkTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(128) NOT NULL,
    [ExpiresAt] datetime2(3) NOT NULL,
    [ConsumedAt] datetime2(3) NULL,
    [CreatedAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_MagicLinkTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MagicLinkTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(128) NOT NULL,
    [InstallationId] nvarchar(128) NOT NULL,
    [ExpiresAt] datetime2(3) NOT NULL,
    [RevokedAt] datetime2(3) NULL,
    [CreatedAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[UserNotificationPreferences] (
    [UserId] uniqueidentifier NOT NULL,
    [InAppToastsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
    [UpdatedAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_UserNotificationPreferences] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_UserNotificationPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[ConversationMembers] (
    [ConversationId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [JoinedAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_ConversationMembers] PRIMARY KEY ([ConversationId], [UserId]),
    CONSTRAINT [FK_ConversationMembers_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[Conversations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ConversationMembers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[ConversationNotificationPreferences] (
    [ConversationId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [IsMuted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [UpdatedAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_ConversationNotificationPreferences] PRIMARY KEY ([ConversationId], [UserId]),
    CONSTRAINT [FK_ConversationNotificationPreferences_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[Conversations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ConversationNotificationPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[DirectMessagePairs] (
    [UserAId] uniqueidentifier NOT NULL,
    [UserBId] uniqueidentifier NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_DirectMessagePairs] PRIMARY KEY ([UserAId], [UserBId]),
    CONSTRAINT [FK_DirectMessagePairs_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[Conversations] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[Messages] (
    [Id] char(26) NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    [SenderId] uniqueidentifier NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL,
    [EditedAt] datetime2(3) NULL,
    [DeletedAt] datetime2(3) NULL,
    [MentionUserIdsJson] nvarchar(max) NOT NULL DEFAULT N'[]',
    [ReactionsJson] nvarchar(max) NOT NULL DEFAULT N'{}',
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Messages_MentionUserIdsJson_IsJson] CHECK (ISJSON([MentionUserIdsJson]) = 1),
    CONSTRAINT [CK_Messages_ReactionsJson_IsJson] CHECK (ISJSON([ReactionsJson]) = 1),
    CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[Conversations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Messages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[ReadStates] (
    [ConversationId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [LastReadAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_ReadStates] PRIMARY KEY ([ConversationId], [UserId]),
    CONSTRAINT [FK_ReadStates_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[Conversations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReadStates_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[Attachments] (
    [Id] uniqueidentifier NOT NULL,
    [MessageId] char(26) NOT NULL,
    [Provider] tinyint NOT NULL,
    [FileId] nvarchar(256) NULL,
    [FileName] nvarchar(512) NOT NULL,
    [ContentType] nvarchar(256) NULL,
    [SizeBytes] bigint NULL,
    [ShareUrl] nvarchar(2048) NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_Attachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attachments_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [dbo].[Messages] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Attachments_FileName] ON [dbo].[Attachments] ([FileName]);

CREATE INDEX [IX_Attachments_MessageId] ON [dbo].[Attachments] ([MessageId]);

CREATE INDEX [IX_ConversationMembers_UserId] ON [dbo].[ConversationMembers] ([UserId]);

CREATE INDEX [IX_ConversationNotificationPreferences_UserId] ON [dbo].[ConversationNotificationPreferences] ([UserId]);

CREATE INDEX [IX_Conversations_CreatedByUserId] ON [dbo].[Conversations] ([CreatedByUserId]);

CREATE INDEX [IX_Conversations_Type] ON [dbo].[Conversations] ([Type]);

CREATE UNIQUE INDEX [IX_DirectMessagePairs_ConversationId] ON [dbo].[DirectMessagePairs] ([ConversationId]);

CREATE INDEX [IX_MagicLinkTokens_TokenHash] ON [dbo].[MagicLinkTokens] ([TokenHash]);

CREATE INDEX [IX_MagicLinkTokens_UserId] ON [dbo].[MagicLinkTokens] ([UserId]);

CREATE INDEX [IX_Messages_ConversationId_CreatedAt_Id] ON [dbo].[Messages] ([ConversationId], [CreatedAt] DESC, [Id] DESC);

CREATE INDEX [IX_Messages_SenderId] ON [dbo].[Messages] ([SenderId]);

CREATE INDEX [IX_ReadStates_UserId] ON [dbo].[ReadStates] ([UserId]);

CREATE INDEX [IX_RefreshTokens_TokenHash] ON [dbo].[RefreshTokens] ([TokenHash]);

CREATE INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens] ([UserId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users] ([Email]);

CREATE INDEX [IX_Users_IsDisabled] ON [dbo].[Users] ([IsDisabled]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260224075312_InitialCreate', N'10.0.0');

COMMIT;
GO

