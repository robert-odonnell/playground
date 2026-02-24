using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FamilyChat.Contracts.Admin;
using FamilyChat.Contracts.Auth;
using FamilyChat.Contracts.Common;
using FamilyChat.Contracts.Conversations;
using FamilyChat.Contracts.Messages;
using FamilyChat.Contracts.Notifications;
using FamilyChat.Contracts.Search;

namespace FamilyChat.ClientSdk.Api;

public sealed class FamilyChatApiClient(HttpClient httpClient)
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public string? AccessToken { get; set; }

    public async Task<ApiAckDto> RequestMagicLinkAsync(MagicLinkRequestDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<ApiAckDto>(HttpMethod.Post, "auth/magic-link/request", request, false, cancellationToken);
    }

    public async Task<AuthTokensDto> VerifyMagicLinkAsync(MagicLinkVerifyDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<AuthTokensDto>(HttpMethod.Post, "auth/magic-link/verify", request, false, cancellationToken);
    }

    public async Task<AuthTokensDto> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<AuthTokensDto>(HttpMethod.Post, "auth/refresh", request, false, cancellationToken);
    }

    public async Task<ApiAckDto> LogoutAsync(LogoutRequestDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<ApiAckDto>(HttpMethod.Post, "auth/logout", request, true, cancellationToken);
    }

    public async Task<AuthMeDto> GetMeAsync(CancellationToken cancellationToken)
    {
        return await SendAsync<AuthMeDto>(HttpMethod.Get, "auth/me", null, true, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(CancellationToken cancellationToken)
    {
        return await SendAsync<IReadOnlyList<ConversationDto>>(HttpMethod.Get, "conversations", null, true, cancellationToken);
    }

    public async Task<ConversationDto> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await SendAsync<ConversationDto>(HttpMethod.Get, $"conversations/{conversationId}", null, true, cancellationToken);
    }

    public async Task<ConversationDto> CreateChannelAsync(CreateChannelRequestDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<ConversationDto>(HttpMethod.Post, "conversations/channel", request, true, cancellationToken);
    }

    public async Task<ConversationDto> CreateDmAsync(CreateDmRequestDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<ConversationDto>(HttpMethod.Post, "conversations/dm", request, true, cancellationToken);
    }

    public async Task<ConversationDto> CreateGroupDmAsync(CreateGroupDmRequestDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<ConversationDto>(HttpMethod.Post, "conversations/groupdm", request, true, cancellationToken);
    }

    public async Task<ConversationDto> UpdateConversationAsync(
        Guid conversationId,
        UpdateConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        return await SendAsync<ConversationDto>(
            HttpMethod.Patch,
            $"conversations/{conversationId}",
            request,
            true,
            cancellationToken);
    }

    public async Task<ConversationDto> JoinConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await SendAsync<ConversationDto>(HttpMethod.Post, $"conversations/{conversationId}/join", null, true, cancellationToken);
    }

    public async Task<ApiAckDto> LeaveConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await SendAsync<ApiAckDto>(HttpMethod.Post, $"conversations/{conversationId}/leave", null, true, cancellationToken);
    }

    public async Task<ConversationMemberDto> AddConversationMemberAsync(
        Guid conversationId,
        AddMemberRequestDto request,
        CancellationToken cancellationToken)
    {
        return await SendAsync<ConversationMemberDto>(
            HttpMethod.Post,
            $"conversations/{conversationId}/members",
            request,
            true,
            cancellationToken);
    }

    public async Task<ApiAckDto> RemoveConversationMemberAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        return await SendAsync<ApiAckDto>(
            HttpMethod.Delete,
            $"conversations/{conversationId}/members/{userId}",
            null,
            true,
            cancellationToken);
    }

    public async Task<PagedResultDto<MessageDto>> GetMessagesAsync(
        Guid conversationId,
        string? before,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = $"conversations/{conversationId}/messages?limit={limit}";
        if (!string.IsNullOrWhiteSpace(before))
        {
            query += $"&before={Uri.EscapeDataString(before)}";
        }

        return await SendAsync<PagedResultDto<MessageDto>>(HttpMethod.Get, query, null, true, cancellationToken);
    }

    public async Task<MessageDto> CreateMessageAsync(
        Guid conversationId,
        CreateMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        return await SendAsync<MessageDto>(HttpMethod.Post, $"conversations/{conversationId}/messages", request, true, cancellationToken);
    }

    public async Task<MessageDto> UpdateMessageAsync(string messageId, UpdateMessageRequestDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<MessageDto>(HttpMethod.Patch, $"messages/{messageId}", request, true, cancellationToken);
    }

    public async Task<ApiAckDto> DeleteMessageAsync(string messageId, CancellationToken cancellationToken)
    {
        return await SendAsync<ApiAckDto>(HttpMethod.Delete, $"messages/{messageId}", null, true, cancellationToken);
    }

    public async Task<MessageDto> ToggleReactionAsync(string messageId, string emoji, CancellationToken cancellationToken)
    {
        return await SendAsync<MessageDto>(
            HttpMethod.Put,
            $"messages/{messageId}/reactions/{Uri.EscapeDataString(emoji)}",
            null,
            true,
            cancellationToken);
    }

    public async Task<UserNotificationPreferenceDto> GetNotificationPreferencesAsync(CancellationToken cancellationToken)
    {
        return await SendAsync<UserNotificationPreferenceDto>(HttpMethod.Get, "notifications/preferences", null, true, cancellationToken);
    }

    public async Task<UserNotificationPreferenceDto> UpdateNotificationPreferencesAsync(
        UserNotificationPreferenceDto request,
        CancellationToken cancellationToken)
    {
        return await SendAsync<UserNotificationPreferenceDto>(HttpMethod.Put, "notifications/preferences", request, true, cancellationToken);
    }

    public async Task<ConversationNotificationPreferenceDto> GetConversationNotificationAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await SendAsync<ConversationNotificationPreferenceDto>(
            HttpMethod.Get,
            $"conversations/{conversationId}/notification",
            null,
            true,
            cancellationToken);
    }

    public async Task<ConversationNotificationPreferenceDto> UpdateConversationNotificationAsync(
        Guid conversationId,
        ConversationNotificationPreferenceDto request,
        CancellationToken cancellationToken)
    {
        return await SendAsync<ConversationNotificationPreferenceDto>(
            HttpMethod.Put,
            $"conversations/{conversationId}/notification",
            request,
            true,
            cancellationToken);
    }

    public async Task<ApiAckDto> UpdateReadStateAsync(
        Guid conversationId,
        UpdateReadStateRequestDto request,
        CancellationToken cancellationToken)
    {
        return await SendAsync<ApiAckDto>(
            HttpMethod.Put,
            $"conversations/{conversationId}/read",
            request,
            true,
            cancellationToken);
    }

    public async Task<SearchResponseDto> SearchAsync(string query, Guid? conversationId, CancellationToken cancellationToken)
    {
        var path = $"search?q={Uri.EscapeDataString(query)}";
        if (conversationId.HasValue)
        {
            path += $"&conversationId={conversationId.Value}";
        }

        return await SendAsync<SearchResponseDto>(HttpMethod.Get, path, null, true, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetAdminUsersAsync(CancellationToken cancellationToken)
    {
        return await SendAsync<IReadOnlyList<AdminUserDto>>(HttpMethod.Get, "admin/users", null, true, cancellationToken);
    }

    public async Task<AdminUserDto> AddAdminUserAsync(AdminCreateUserRequestDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<AdminUserDto>(HttpMethod.Post, "admin/users", request, true, cancellationToken);
    }

    public async Task<AdminUserDto> UpdateAdminUserAsync(
        Guid userId,
        AdminUpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        return await SendAsync<AdminUserDto>(HttpMethod.Patch, $"admin/users/{userId}", request, true, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminChannelDto>> GetAdminChannelsAsync(CancellationToken cancellationToken)
    {
        return await SendAsync<IReadOnlyList<AdminChannelDto>>(HttpMethod.Get, "admin/channels", null, true, cancellationToken);
    }

    public async Task<AdminChannelDto> AddAdminChannelAsync(CreateChannelRequestDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<AdminChannelDto>(HttpMethod.Post, "admin/channels", request, true, cancellationToken);
    }

    public async Task<AdminChannelDto> UpdateAdminChannelAsync(
        Guid channelId,
        UpdateConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        return await SendAsync<AdminChannelDto>(HttpMethod.Patch, $"admin/channels/{channelId}", request, true, cancellationToken);
    }

    public async Task<ApiAckDto> AddAdminChannelMemberAsync(Guid channelId, AddMemberRequestDto request, CancellationToken cancellationToken)
    {
        return await SendAsync<ApiAckDto>(HttpMethod.Post, $"admin/channels/{channelId}/members", request, true, cancellationToken);
    }

    public async Task<ApiAckDto> RemoveAdminChannelMemberAsync(Guid channelId, Guid userId, CancellationToken cancellationToken)
    {
        return await SendAsync<ApiAckDto>(HttpMethod.Delete, $"admin/channels/{channelId}/members/{userId}", null, true, cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);

        if (requiresAuth)
        {
            if (string.IsNullOrWhiteSpace(AccessToken))
            {
                throw new InvalidOperationException("Access token is not set.");
            }

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
        }

        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, _jsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (typeof(T) == typeof(ApiAckDto) && response.Content.Headers.ContentLength == 0)
        {
            return (T)(object)new ApiAckDto();
        }

        var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned empty payload.");

        return result;
    }
}
