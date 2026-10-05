using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace Navegador.Services;

internal sealed class BrowserUpdateService
{
    private const string ReleaseApiUrl =
        "https://api.github.com/repos/joaoldsxyzbr/Navegador/releases/latest";
    private const long MaximumPackageSize = 512L * 1024L * 1024L;

    private static readonly HttpClient Client = CreateClient();

    public async Task<UpdateCheckResult> CheckLatestAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ReleaseApiUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await Client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return UpdateCheckResult.NoRelease();

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
        var versionText = tag.TrimStart('v', 'V');

        if (!Version.TryParse(versionText, out var latestVersion) ||
            !Version.TryParse(AppInfo.Current.VersionString, out var currentVersion))
        {
            throw new InvalidDataException("A versão publicada ou instalada está inválida.");
        }

        if (latestVersion <= currentVersion)
            return UpdateCheckResult.UpToDate(tag);

        const string windowsAssetName = "Navegador-windows-x64.zip";
        const string androidAssetName = "Navegador-android.apk";
        var expectedAssetName = DeviceInfo.Platform == DevicePlatform.Android
            ? androidAssetName
            : windowsAssetName;

        var assets = root.GetProperty("assets").EnumerateArray();
        var asset = assets.FirstOrDefault(item =>
            string.Equals(
                item.GetProperty("name").GetString(),
                expectedAssetName,
                StringComparison.Ordinal));

        if (asset.ValueKind == JsonValueKind.Undefined)
            return UpdateCheckResult.MissingPackage(tag, root.GetProperty("body").GetString());

        var url = asset.GetProperty("browser_download_url").GetString();
        var digest = asset.TryGetProperty("digest", out var digestElement)
            ? digestElement.GetString()
            : null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var packageUri) ||
            packageUri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(packageUri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("O endereço do pacote publicado não é confiável.");
        }

        if (string.IsNullOrWhiteSpace(digest) ||
            !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            return UpdateCheckResult.MissingDigest(
                tag,
                root.GetProperty("body").GetString());
        }

        return UpdateCheckResult.Available(
            tag,
            root.GetProperty("body").GetString(),
            packageUri,
            digest["sha256:".Length..]);
    }

    public async Task<string> DownloadAndVerifyAsync(
        UpdateCheckResult update,
        CancellationToken cancellationToken = default)
    {
        if (!update.HasVerifiedPackage || update.PackageUri is null)
            throw new InvalidOperationException("O pacote não possui validação de integridade.");

        using var response = await Client.GetAsync(
            update.PackageUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is > MaximumPackageSize)
            throw new InvalidDataException("O pacote excede o tamanho permitido.");

        var extension = DeviceInfo.Platform == DevicePlatform.Android ? ".apk" : ".zip";
        var directory = Path.Combine(FileSystem.CacheDirectory, "updates");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, $"Navegador-{Guid.NewGuid():N}{extension}");

        try
        {
            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = File.Create(destination))
            {
                var buffer = new byte[81920];
                long totalBytes = 0;
                int bytesRead;

                while ((bytesRead = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    totalBytes += bytesRead;

                    if (totalBytes > MaximumPackageSize)
                        throw new InvalidDataException("O pacote excede o tamanho permitido.");

                    await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }
            }

            await using var package = File.OpenRead(destination);
            var actualDigest = Convert.ToHexString(await SHA256.HashDataAsync(package, cancellationToken));

            if (!string.Equals(actualDigest, update.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A validação de integridade do pacote falhou.");

            return destination;
        }
        catch
        {
            if (File.Exists(destination))
                File.Delete(destination);

            throw;
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Navegador/0.1");
        return client;
    }
}

internal sealed record UpdateCheckResult(
    UpdateStatus Status,
    string? Version,
    string? ReleaseNotes,
    Uri? PackageUri,
    string? Sha256,
    bool HasVerifiedPackage)
{
    public static UpdateCheckResult NoRelease() =>
        new(UpdateStatus.NoRelease, null, null, null, null, false);

    public static UpdateCheckResult UpToDate(string version) =>
        new(UpdateStatus.UpToDate, version, null, null, null, false);

    public static UpdateCheckResult MissingPackage(string version, string? notes) =>
        new(UpdateStatus.MissingPackage, version, notes, null, null, false);

    public static UpdateCheckResult MissingDigest(string version, string? notes) =>
        new(UpdateStatus.MissingDigest, version, notes, null, null, false);

    public static UpdateCheckResult Available(
        string version,
        string? notes,
        Uri packageUri,
        string sha256) =>
        new(UpdateStatus.Available, version, notes, packageUri, sha256, true);
}

internal enum UpdateStatus
{
    NoRelease,
    UpToDate,
    MissingPackage,
    MissingDigest,
    Available
}
