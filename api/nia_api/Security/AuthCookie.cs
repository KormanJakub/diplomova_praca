namespace nia_api.Security;

public static class AuthCookie
{
    public const string Name = "__Host-waffl_session";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    public static CookieOptions CreateOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = "/",
        MaxAge = Lifetime,
        IsEssential = true
    };
}
