namespace Navegador.Core;

public static class AddressResolver
{
    public const string HomeUrl = "https://www.google.com/";

    public static string Resolve(string? input)
    {
        var value = input?.Trim();

        if (string.IsNullOrWhiteSpace(value))
            return HomeUrl;

        if (TryGetHttpUri(value, out var directUri))
            return directUri.AbsoluteUri;

        if (LooksLikeHost(value) && TryGetHttpUri($"https://{value}", out var hostUri))
            return hostUri.AbsoluteUri;

        return $"https://www.google.com/search?q={Uri.EscapeDataString(value)}";
    }

    private static bool TryGetHttpUri(string value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
        {
            uri = parsed;
            return true;
        }

        uri = null!;
        return false;
    }

    private static bool LooksLikeHost(string value)
    {
        if (value.Contains(' '))
            return false;

        return value.Contains('.') ||
               value.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }
}
