using nia_api.Security;
using Microsoft.AspNetCore.Http;

namespace nia_api.Tests;

public class SecurityTests
{
    [Fact]
    public void CapabilityTokens_AreRandomAndOnlyHashesAreStored()
    {
        var first = CapabilityToken.Create();
        var second = CapabilityToken.Create();

        Assert.NotEqual(first, second);
        Assert.Equal(64, first.Length);
        Assert.True(CapabilityToken.IsHash(CapabilityToken.Hash(first)));
        Assert.NotEqual(first, CapabilityToken.Hash(first));
    }

    [Fact]
    public void AuthenticationCookie_IsHttpOnlySecureAndCrossSiteCompatible()
    {
        var options = AuthCookie.CreateOptions();

        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.None, options.SameSite);
        Assert.Equal("/", options.Path);
        Assert.Equal(AuthCookie.Lifetime, options.MaxAge);
    }

    [Fact]
    public void EmailNormalization_IsCaseAndWhitespaceInsensitive()
    {
        Assert.Equal("USER@EXAMPLE.COM", EmailNormalizer.Normalize("  User@Example.com "));
    }
}
