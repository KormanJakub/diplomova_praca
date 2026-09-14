using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using nia_api.Models;

namespace nia_api.Services;

public class PasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(string password) => _hasher.HashPassword(new User(), password);

    public bool VerifyPassword(string enteredPassword, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash)) return false;
        if (IsLegacyHash(storedHash))
        {
            var legacyHash = SHA256.HashData(Encoding.UTF8.GetBytes(enteredPassword));
            return CryptographicOperations.FixedTimeEquals(legacyHash, Convert.FromBase64String(storedHash));
        }

        try
        {
            return _hasher.VerifyHashedPassword(new User(), storedHash, enteredPassword)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool IsLegacyHash(string storedHash)
    {
        try { return Convert.FromBase64String(storedHash).Length == 32; }
        catch (FormatException) { return false; }
    }
}
