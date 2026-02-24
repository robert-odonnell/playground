using System.Security.Cryptography;
using System.Text;

namespace FamilyChat.Application.Utils;

public static class CryptoHelpers
{
    public static string HashToken(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }
}
