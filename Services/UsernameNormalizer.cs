namespace GuangYingTongTu.Api.Services;

public static class UsernameNormalizer
{
    public static string Normalize(string username)
    {
        return username.Trim().ToLowerInvariant();
    }
}
