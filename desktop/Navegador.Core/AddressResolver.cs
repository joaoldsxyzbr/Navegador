namespace Navegador.Core;

/// <summary>
/// Traduz o que o usuário digitou na barra de endereço em uma URL navegável.
/// Regra: HTTP/HTTPS explícito ou domínio plausível vira URL; o resto vira busca.
/// </summary>
public static class AddressResolver
{
    public const string SearchPrefix = "https://www.google.com/search?q=";

    public static string Resolve(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var value = input.Trim();

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute) &&
            (IsWebScheme(absolute) || IsKnownLocalScheme(absolute)))
        {
            return absolute.ToString();
        }

        if (LooksLikeHost(value)) return "https://" + value;

        return SearchPrefix + Uri.EscapeDataString(value);
    }

    /// <summary>Verdadeiro quando a URL é segura para persistir e reabrir.</summary>
    public static bool IsPersistable(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (IsWebScheme(uri) || IsKnownLocalScheme(uri));
    }

    public static string HostOf(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : string.Empty;
    }

    private static bool IsWebScheme(Uri uri) =>
        uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static bool IsKnownLocalScheme(Uri uri) =>
        uri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase) ||
        uri.Scheme.Equals("edge", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeHost(string value)
    {
        if (value.Any(char.IsWhiteSpace)) return false;

        if (value.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)) return true;

        // Um ponto com algo dos dois lados já é tratado como domínio.
        var separator = value.IndexOf('.');
        return separator > 0 && separator < value.Length - 1;
    }
}
