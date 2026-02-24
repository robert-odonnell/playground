SET NOCOUNT ON;

DECLARE @AdminUserId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @GeneralChannelId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @AdminUserId)
BEGIN
    INSERT INTO dbo.Users (Id, Email, DisplayName, IsAdmin, IsDisabled, CreatedAt)
    VALUES (@AdminUserId, N'admin@familychat.local', N'Family Admin', 1, 0, @Now);
END

IF NOT EXISTS (SELECT 1 FROM dbo.Conversations WHERE Id = @GeneralChannelId)
BEGIN
    INSERT INTO dbo.Conversations (Id, Type, Name, Topic, IsPrivate, CreatedByUserId, CreatedAt)
    VALUES (@GeneralChannelId, 0, N'general', N'Family general chat', 0, @AdminUserId, @Now);
END

IF NOT EXISTS (SELECT 1 FROM dbo.ConversationMembers WHERE ConversationId = @GeneralChannelId AND UserId = @AdminUserId)
BEGIN
    INSERT INTO dbo.ConversationMembers (ConversationId, UserId, JoinedAt)
    VALUES (@GeneralChannelId, @AdminUserId, @Now);
END

IF NOT EXISTS (SELECT 1 FROM dbo.ReadStates WHERE ConversationId = @GeneralChannelId AND UserId = @AdminUserId)
BEGIN
    INSERT INTO dbo.ReadStates (ConversationId, UserId, LastReadAt)
    VALUES (@GeneralChannelId, @AdminUserId, @Now);
END
