using FamilyChat.Application.Utils;
using FluentAssertions;

namespace FamilyChat.UnitTests;

public sealed class MentionParserTests
{
    [Fact]
    public void ExtractMentionTokens_ShouldReturnDistinctTokens()
    {
        var body = "Hi @Robert and @robert, also ping @anna.smith and plain text.";

        var tokens = MessageJson.ExtractMentionTokens(body);

        tokens.Should().BeEquivalentTo(["Robert", "anna.smith"]);
    }
}
