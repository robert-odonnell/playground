using System.Security.Cryptography;
using System.Text;

namespace FamilyChat.Application.Utils;

public static class CryptoHelpers
{
    private const string MagicCodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string HashToken(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }

    public static string CreateMagicCode(int length = 6)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");
        }

        Span<char> chars = stackalloc char[length];
        for (var index = 0; index < chars.Length; index++)
        {
            chars[index] = MagicCodeAlphabet[RandomNumberGenerator.GetInt32(MagicCodeAlphabet.Length)];
        }

        return new string(chars);
    }

    public static string NormalizeMagicCode(string value) => value.Trim().ToUpperInvariant();
}
