using System.Security.Cryptography;
using System.Text;
using nia_api.Services;

namespace nia_api.Tests;

public class PasswordServiceTests
{
    [Fact]
    public void NewHashesAreSaltedAndVerifiable()
    {
        var service = new PasswordService();
        var first = service.HashPassword("Correct-Horse-123");
        var second = service.HashPassword("Correct-Horse-123");

        Assert.NotEqual(first, second);
        Assert.True(service.VerifyPassword("Correct-Horse-123", first));
        Assert.False(service.VerifyPassword("Wrong-Password-123", first));
        Assert.False(service.IsLegacyHash(first));
    }

    [Fact]
    public void LegacySha256HashCanBeVerifiedForMigration()
    {
        var service = new PasswordService();
        var legacy = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("Old-Password-123")));

        Assert.True(service.IsLegacyHash(legacy));
        Assert.True(service.VerifyPassword("Old-Password-123", legacy));
        Assert.False(service.VerifyPassword("Wrong-Password-123", legacy));
    }
}
