namespace FamilyChat.Contracts.Realtime;

public static class RealtimeEvents
{
    public const string MessageCreated = "message.created";
    public const string MessageUpdated = "message.updated";
    public const string MessageDeleted = "message.deleted";
    public const string MessageReactionUpdated = "message.reactionUpdated";
    public const string ConversationUpdated = "conversation.updated";
    public const string MemberJoined = "member.joined";
    public const string MemberLeft = "member.left";
    public const string UnreadUpdated = "unread.updated";
}
