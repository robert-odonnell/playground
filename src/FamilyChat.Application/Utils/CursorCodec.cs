using System.Text;
using System.Text.Json;

namespace FamilyChat.Application.Utils;

public static class CursorCodec
{
    private sealed class CursorModel
    {
        public long CreatedAtTicks { get; set; }
        public string MessageId { get; set; } = string.Empty;
    }

    public static string Encode(DateTime createdAtUtc, string messageId)
    {
        var model = new CursorModel
        {
            CreatedAtTicks = createdAtUtc.Ticks,
            MessageId = messageId
        };

        var json = JsonSerializer.Serialize(model);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static (DateTime CreatedAtUtc, string MessageId)? Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            var model = JsonSerializer.Deserialize<CursorModel>(json);
            if (model is null || string.IsNullOrWhiteSpace(model.MessageId))
            {
                return null;
            }

            return (new DateTime(model.CreatedAtTicks, DateTimeKind.Utc), model.MessageId);
        }
        catch
        {
            return null;
        }
    }
}
