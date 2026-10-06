using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Navegador.Windows;

internal static class UpdateService
{
    private const string LatestReleaseApi = "https://api.github.com/repos/joaoldsxyzbr/Navegador/releases/latest";
    private const string UpdateArgument = "--apply-update";
    private const string PackageSuffix = "-windows-x64.zip";

    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        AllowAutoRedirect = true
    })
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    public static async Task CheckAndInstallAsync(IWin32Window owner)
    {
        try
        {
            var release = await GetLatestReleaseAsync();
            var current = GetCurrentVersion();

            if (release.Version <= current)
            {
                MessageBox.Show(
                    owner,
                    $"Você já está usando a versão mais recente do Navegador ({FormatVersion(current)}).",
                    "Atualizar Navegador",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var sizeText = release.SizeBytes > 0
                ? $"\nTamanho: {release.SizeBytes / 1024d / 1024d:0.0} MB"
                : string.Empty;

            var confirmation = MessageBox.Show(
                owner,
                $"Uma nova versão está disponível.\n\n" +
                $"Atual: {FormatVersion(current)}\n" +
                $"Nova: {FormatVersion(release.Version)}" +
                sizeText +
                "\n\nBaixar e instalar agora?",
                "Atualizar Navegador",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes) return;

            var updateDirectory = CreateUpdateDirectory(release.Version);
            var packagePath = Path.Combine(updateDirectory, $"Navegador-{release.TagName}{PackageSuffix}");

            await DownloadFileAsync(release.DownloadUrl, packagePath);
            await ValidatePackageAsync(packagePath, release.ExpectedSha256, release.ChecksumUrl);

            var currentExecutable = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(currentExecutable) || !File.Exists(currentExecutable))
                throw new InvalidOperationException("Não foi possível localizar o executável atual do Navegador.");

            var helperPath = Path.Combine(updateDirectory, "Navegador.UpdateHelper.exe");
            File.Copy(currentExecutable, helperPath, overwrite: true);

            var installDirectory = Path.GetFullPath(AppContext.BaseDirectory);
            var startInfo = new ProcessStartInfo
            {
                FileName = helperPath,
                WorkingDirectory = updateDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(UpdateArgument);
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add(packagePath);
            startInfo.ArgumentList.Add(installDirectory);

            if (Process.Start(startInfo) is null)
                throw new InvalidOperationException("Não foi possível iniciar o instalador da atualização.");

            Application.Exit();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                owner,
                "Não foi possível atualizar o Navegador.\n\n" + exception.Message,
                "Atualizar Navegador",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    public static bool TryRunApplyUpdate(string[] args)
    {
        if (args.Length == 0 || !args[0].Equals(UpdateArgument, StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            if (args.Length != 4)
                throw new ArgumentException("Parâmetros da atualização inválidos.");

            if (!int.TryParse(args[1], out var parentProcessId))
                throw new ArgumentException("PID do Navegador inválido.");

            var packagePath = Path.GetFullPath(args[2]);
            var installDirectory = Path.GetFullPath(args[3]);

            ApplyUpdate(parentProcessId, packagePath, installDirectory);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "A atualização não pôde ser concluída.\n\n" + exception.Message +
                "\n\nAbra o Navegador novamente e tente atualizar.",
                "Navegador",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        return true;
    }

    public static void CleanupOldUpdateCache()
    {
        try
        {
            var root = GetUpdateRoot();
            if (!Directory.Exists(root)) return;

            foreach (var directory in Directory.EnumerateDirectories(root))
            {
                try
                {
                    if (Directory.GetCreationTimeUtc(directory) < DateTime.UtcNow.AddDays(-1))
                        Directory.Delete(directory, recursive: true);
                }
                catch
                {
                    // Um helper ainda em execução pode manter a pasta bloqueada.
                }
            }
        }
        catch
        {
            // Limpeza de cache nunca deve impedir o navegador de abrir.
        }
    }

    private static async Task<ReleaseInfo> GetLatestReleaseAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApi);
        request.Headers.UserAgent.ParseAdd("Navegador-Windows");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        var root = json.RootElement;
        var tagName = root.GetProperty("tag_name").GetString()
            ?? throw new InvalidOperationException("A release mais recente não possui tag.");

        if (!Version.TryParse(tagName.TrimStart('v', 'V'), out var version))
            throw new InvalidOperationException($"Versão de release inválida: {tagName}.");

        string? downloadUrl = null;
        string? expectedSha256 = null;
        string? checksumUrl = null;
        long sizeBytes = 0;
        string? packageName = null;

        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? string.Empty;

            if (name.EndsWith(PackageSuffix, StringComparison.OrdinalIgnoreCase))
            {
                packageName = name;
                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                sizeBytes = asset.TryGetProperty("size", out var size) ? size.GetInt64() : 0;

                if (asset.TryGetProperty("digest", out var digestElement))
                {
                    var digest = digestElement.GetString();
                    if (!string.IsNullOrWhiteSpace(digest) &&
                        digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                    {
                        expectedSha256 = digest["sha256:".Length..];
                    }
                }
            }
        }

        if (downloadUrl is null || packageName is null)
            throw new InvalidOperationException("A release mais recente não possui pacote Windows x64.");

        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? string.Empty;
            if (name.Equals(packageName + ".sha256", StringComparison.OrdinalIgnoreCase))
            {
                checksumUrl = asset.GetProperty("browser_download_url").GetString();
                break;
            }
        }

        EnsureGitHubDownloadUrl(downloadUrl);
        if (checksumUrl is not null) EnsureGitHubDownloadUrl(checksumUrl);

        return new ReleaseInfo(
            version,
            tagName,
            downloadUrl,
            expectedSha256,
            checksumUrl,
            sizeBytes);
    }

    private static Version GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
    }

    private static async Task DownloadFileAsync(string url, string destination)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("Navegador-Windows");

        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync();
        await using var target = new FileStream(
            destination,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024 * 128,
            useAsync: true);

        await source.CopyToAsync(target);
    }

    private static async Task ValidatePackageAsync(
        string packagePath,
        string? expectedSha256,
        string? checksumUrl)
    {
        var expected = expectedSha256;

        if (string.IsNullOrWhiteSpace(expected) && checksumUrl is not null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, checksumUrl);
            request.Headers.UserAgent.ParseAdd("Navegador-Windows");

            using var response = await Http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var checksumText = await response.Content.ReadAsStringAsync();
            expected = checksumText
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(expected))
            throw new InvalidOperationException("A release não possui checksum SHA-256 para validação.");

        await using var stream = File.OpenRead(packagePath);
        var hash = await SHA256.HashDataAsync(stream);
        var actual = Convert.ToHexString(hash);

        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O arquivo baixado falhou na validação SHA-256.");
    }

    private static void ApplyUpdate(int parentProcessId, string packagePath, string installDirectory)
    {
        WaitForBrowserToExit(parentProcessId);

        if (!File.Exists(packagePath))
            throw new FileNotFoundException("O pacote de atualização não foi encontrado.", packagePath);

        Directory.CreateDirectory(installDirectory);

        var stagingDirectory = Path.Combine(
            Path.GetDirectoryName(packagePath) ?? Path.GetTempPath(),
            "staging");

        if (Directory.Exists(stagingDirectory))
            Directory.Delete(stagingDirectory, recursive: true);

        Directory.CreateDirectory(stagingDirectory);
        ZipFile.ExtractToDirectory(packagePath, stagingDirectory, overwriteFiles: true);

        var stagedExecutable = Path.Combine(stagingDirectory, "Navegador.exe");
        if (!File.Exists(stagedExecutable))
            throw new InvalidOperationException("O pacote baixado não contém Navegador.exe.");

        foreach (var sourceFile in Directory.EnumerateFiles(stagingDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(stagingDirectory, sourceFile);

            if (relativePath.Equals("Data", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith("Data" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var destinationFile = Path.Combine(installDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            CopyWithRetry(sourceFile, destinationFile);
        }

        var installedExecutable = Path.Combine(installDirectory, "Navegador.exe");
        if (!File.Exists(installedExecutable))
            throw new InvalidOperationException("Navegador.exe não foi encontrado após a atualização.");

        Process.Start(new ProcessStartInfo
        {
            FileName = installedExecutable,
            WorkingDirectory = installDirectory,
            UseShellExecute = true
        });
    }

    private static void WaitForBrowserToExit(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.WaitForExit(60_000))
                throw new TimeoutException("O Navegador demorou demais para encerrar.");
        }
        catch (ArgumentException)
        {
            // O processo já encerrou.
        }

        Thread.Sleep(350);
    }

    private static void CopyWithRetry(string sourceFile, string destinationFile)
    {
        Exception? lastError = null;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                File.Copy(sourceFile, destinationFile, overwrite: true);
                return;
            }
            catch (IOException exception)
            {
                lastError = exception;
                Thread.Sleep(250);
            }
            catch (UnauthorizedAccessException exception)
            {
                lastError = exception;
                Thread.Sleep(250);
            }
        }

        throw new IOException($"Não foi possível substituir {Path.GetFileName(destinationFile)}.", lastError);
    }

    private static string CreateUpdateDirectory(Version version)
    {
        var directory = Path.Combine(
            GetUpdateRoot(),
            $"{FormatVersion(version)}-{Guid.NewGuid():N}");

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string GetUpdateRoot()
    {
        return Path.Combine(Path.GetTempPath(), "Navegador", "Updates");
    }

    private static string FormatVersion(Version version)
    {
        return $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
    }

    private static void EnsureGitHubDownloadUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A release retornou uma URL de download inesperada.");
        }
    }

    private sealed record ReleaseInfo(
        Version Version,
        string TagName,
        string DownloadUrl,
        string? ExpectedSha256,
        string? ChecksumUrl,
        long SizeBytes);
}
