using System.Text.Json;

namespace Navegador.Core.Updates;

/// <summary>Resumo do arquivo de release publicado no GitHub.</summary>
public sealed record ReleaseInfo(
    Version Version,
    string TagName,
    string PackageName,
    string DownloadUrl,
    string? ExpectedSha256,
    string? ChecksumUrl,
    long SizeBytes)
{
    /// <summary>Versão no formato de três partes, como aparece na interface.</summary>
    public string DisplayVersion => VersionFormatter.ToDisplay(Version);
}

public static class VersionFormatter
{
    /// <summary>
    /// Compara apenas as três partes visíveis. A versão do assembly tem quatro
    /// (0.4.0.0) e a tag da release tem três (v0.4.0), então comparar os objetos
    /// Version crus acusaria uma atualização inexistente.
    /// </summary>
    public static int Compare(Version left, Version right)
    {
        var leftValue = (Math.Max(left.Major, 0), Math.Max(left.Minor, 0), Math.Max(left.Build, 0));
        var rightValue = (Math.Max(right.Major, 0), Math.Max(right.Minor, 0), Math.Max(right.Build, 0));
        return leftValue.CompareTo(rightValue);
    }

    public static string ToDisplay(Version version) =>
        $"{Math.Max(version.Major, 0)}.{Math.Max(version.Minor, 0)}.{Math.Max(version.Build, 0)}";

    public static bool TryParseTag(string? tag, out Version version)
    {
        version = new Version(0, 0, 0);

        if (string.IsNullOrWhiteSpace(tag)) return false;

        var trimmed = tag.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(trimmed, out var parsed)) return false;

        version = new Version(Math.Max(parsed.Major, 0), Math.Max(parsed.Minor, 0), Math.Max(parsed.Build, 0));
        return true;
    }
}

/// <summary>Lê o JSON de <c>releases/latest</c> sem depender de tipos da interface.</summary>
public static class ReleaseParser
{
    public const string PackageSuffix = "-windows-x64.zip";

    private const string AllowedHost = "github.com";

    public static ReleaseInfo Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("tag_name", out var tagElement) ||
            !VersionFormatter.TryParseTag(tagElement.GetString(), out var version))
        {
            throw new InvalidOperationException("A release mais recente não possui uma tag de versão válida.");
        }

        var tagName = tagElement.GetString()!.Trim();

        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("A release mais recente não possui arquivos anexados.");

        string? packageName = null;
        string? downloadUrl = null;
        string? expectedSha256 = null;
        long sizeBytes = 0;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            if (name is null || !name.EndsWith(PackageSuffix, StringComparison.OrdinalIgnoreCase)) continue;

            packageName = name;
            downloadUrl = asset.TryGetProperty("browser_download_url", out var urlElement)
                ? urlElement.GetString()
                : null;
            sizeBytes = asset.TryGetProperty("size", out var sizeElement) ? sizeElement.GetInt64() : 0;

            if (asset.TryGetProperty("digest", out var digestElement))
            {
                var digest = digestElement.GetString();
                if (!string.IsNullOrWhiteSpace(digest) &&
                    digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                {
                    expectedSha256 = digest["sha256:".Length..].Trim();
                }
            }
        }

        if (packageName is null || string.IsNullOrWhiteSpace(downloadUrl))
            throw new InvalidOperationException("A release mais recente não possui pacote Windows x64.");

        string? checksumUrl = null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            if (name is not null &&
                name.Equals(packageName + ".sha256", StringComparison.OrdinalIgnoreCase) &&
                asset.TryGetProperty("browser_download_url", out var urlElement))
            {
                checksumUrl = urlElement.GetString();
                break;
            }
        }

        EnsureGitHubHttpsUrl(downloadUrl);
        if (!string.IsNullOrWhiteSpace(checksumUrl)) EnsureGitHubHttpsUrl(checksumUrl);

        return new ReleaseInfo(
            version,
            tagName,
            packageName,
            downloadUrl,
            expectedSha256,
            checksumUrl,
            sizeBytes);
    }

    /// <summary>Lê o primeiro campo de um arquivo <c>.sha256</c>.</summary>
    public static string? ParseChecksumFile(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        var candidate = content
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .Trim();

        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length != 64) return null;

        return candidate.All(Uri.IsHexDigit) ? candidate : null;
    }

    /// <summary>Recusa qualquer URL de download que não seja HTTPS em github.com.</summary>
    public static void EnsureGitHubHttpsUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.Equals(AllowedHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A release retornou uma URL de download inesperada.");
        }
    }
}
