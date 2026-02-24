using FamilyChat.Application.Abstractions;

namespace FamilyChat.IntegrationTests.Helpers;

public sealed class TestUlidGenerator : IUlidGenerator
{
    private int _counter;

    public string NewUlid()
    {
        _counter++;
        return $"01HFYK4PZQ7QEVJDH85G{_counter:000000}".PadRight(26, '0').Substring(0, 26);
    }
}
