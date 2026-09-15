namespace nia_api.Security;

public static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
