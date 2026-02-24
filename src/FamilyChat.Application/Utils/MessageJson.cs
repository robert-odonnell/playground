using System.Text.Json;
using System.Text.RegularExpressions;

namespace FamilyChat.Application.Utils;

public static partial class MessageJson
{
    [GeneratedRegex(@"(?<!\w)@([A-Za-z0-9._-]{1,64})", RegexOptions.Compiled)]
    private static partial Regex MentionRegex();

    public static IReadOnlyList<string> ExtractMentionTokens(string body)
    {
        var tokens = MentionRegex().Matches(body)
            .Select(match => match.Groups[1].Value.Trim())
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return tokens;
    }

    public static IReadOnlyList<Guid> ParseMentions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var result = JsonSerializer.Deserialize<List<Guid>>(json);
        return result ?? [];
    }

    public static string SerializeMentions(IEnumerable<Guid> mentions)
    {
        return JsonSerializer.Serialize(mentions.Distinct());
    }

    public static Dictionary<string, Guid[]> ParseReactions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, Guid[]>>(json);
        return parsed ?? [];
    }

    public static string SerializeReactions(Dictionary<string, Guid[]> reactions)
    {
        return JsonSerializer.Serialize(reactions);
    }
}
