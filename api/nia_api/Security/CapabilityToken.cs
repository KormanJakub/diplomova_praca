using System.Security.Cryptography;
using System.Text;

namespace nia_api.Security;

public static class CapabilityToken
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool IsHash(string? value) => value is { Length: 64 } &&
        value.All(Uri.IsHexDigit);
}
