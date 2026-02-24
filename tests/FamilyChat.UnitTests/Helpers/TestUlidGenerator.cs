using FamilyChat.Application.Abstractions;

namespace FamilyChat.UnitTests.Helpers;

public sealed class TestUlidGenerator : IUlidGenerator
{
    private int _counter;

    public string NewUlid()
    {
        _counter++;
        return $"01TESTULID{_counter:000000000000000}".PadRight(26, '0').Substring(0, 26);
    }
}
