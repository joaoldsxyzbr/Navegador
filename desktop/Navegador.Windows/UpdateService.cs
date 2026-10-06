using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using Navegador.Core;
using Navegador.Core.Storage;
using Navegador.Core.Updates;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

/// <summary>
/// Atualização pelo GitHub Releases.
///
/// O processo principal baixa o pacote e inicia um processo auxiliar (o próprio
/// <c>Navegador.exe</c> rodando fora da pasta de instalação) para trocar os
/// arquivos. O auxiliar <b>não confia</b> nos argumentos recebidos: ele só age
/// se encontrar um combinado gravado pelo processo pai, se o destino for a
/// própria pasta da instalação e se o hash do pacote bater. Sem isso, qualquer
/// processo do usuário poderia pedir para o Navegador sobrescrever qualquer
/// pasta com um ZIP forjado.
/// </summary>
internal static class UpdateService
{
    private const string LatestReleaseApi = "https://api.github.com/repos/joaoldsxyzbr/Navegador/releases/latest";
    private const string UpdateArgument = "--apply-update";

    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        AllowAutoRedirect = true
    })
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public static async Task CheckAndInstallAsync(IWin32Window owner)
    {
        try
        {
            var release = await GetLatestReleaseAsync();
            var current = CurrentVersion.Value;

            if (VersionFormatter.Compare(release.Version, current) <= 0)
            {
                MessageBox.Show(
                    owner,
                    $"Você já está usando a versão mais recente do {Branding.Name} ({VersionFormatter.ToDisplay(current)}).",
                    $"Atualizar {Branding.Name}",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var sizeText = release.SizeBytes > 0
                ? $"\nTamanho: {release.SizeBytes / 1024d / 1024d:0.0} MB"
                : string.Empty;

            var confirmation = MessageBox.Show(
                owner,
                "Uma nova versão está disponível.\n\n" +
                $"Atual: {VersionFormatter.ToDisplay(current)}\n" +
                $"Nova: {release.DisplayVersion}" +
                sizeText +
                $"\n\nBaixar e instalar agora? O {Branding.Name} será reiniciado.",
                $"Atualizar {Branding.Name}",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes) return;

            var installDirectory = UpdateApplyPolicy.RequireInstallDirectory();
            var executable = Environment.ProcessPath;

            if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
                throw new InvalidOperationException("Não foi possível localizar o executável atual do Navegador.");

            var updateDirectory = UpdatePaths.CreateUpdateDirectory(release.DisplayVersion);
            var packagePath = UpdatePaths.PackageFile(updateDirectory);

            await DownloadFileAsync(release.DownloadUrl, packagePath);
            var packageHash = await ValidatePackageAsync(packagePath, release.ExpectedSha256, release.ChecksumUrl);

            var helperPath = UpdatePaths.HelperFile(updateDirectory);
            try { File.Copy(executable, helperPath, overwrite: true); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException("Não foi possível preparar o auxiliar da atualização.", exception);
            }

            var handshake = UpdateHandshake.Create(
                Environment.ProcessId,
                installDirectory,
                packageHash,
                release.DisplayVersion);

            WriteHandshake(updateDirectory, handshake);

            var startInfo = new ProcessStartInfo
            {
                FileName = helperPath,
                WorkingDirectory = updateDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(UpdateArgument);
            startInfo.ArgumentList.Add(handshake.Token);
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add(packagePath);
            startInfo.ArgumentList.Add(installDirectory);

            var helperProcess = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Não foi possível iniciar o instalador da atualização.");
            WaitForHelperReady(updateDirectory, handshake.Token, helperProcess);
            Application.Exit();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                owner,
                $"Não foi possível atualizar o {Branding.Name}.\n\n" + exception.Message,
                $"Atualizar {Branding.Name}",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Segunda fase, já como processo auxiliar. Devolve <c>true</c> quando os
    /// argumentos indicam uma atualização e o processo deve terminar aqui.
    /// </summary>
    public static bool TryRunApplyUpdate(string[] args)
    {
        if (args.Length == 0 || !args[0].Equals(UpdateArgument, StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            if (args.Length != 5)
                throw new ArgumentException("Parâmetros da atualização inválidos. Invoque a atualização pelo próprio Navegador.");

            var token = args[1];

            if (!int.TryParse(args[2], out var parentProcessId))
                throw new ArgumentException("PID do Navegador inválido.");

            var packagePath = Path.GetFullPath(args[3]);
            var installDirectory = Path.GetFullPath(args[4]);

            // O auxiliar não é um instalador de uso geral: só continua se o
            // combinado gravado pelo processo pai estiver íntegro e for o mesmo.
            var updateDirectory = Path.GetDirectoryName(packagePath)
                ?? throw new InvalidOperationException("Caminho do pacote inválido.");

            var handshake = ReadHandshake(updateDirectory);

            if (!handshake.MatchesToken(token))
                throw new InvalidOperationException("O combinado da atualização não confere. Inicie a atualização pelo Navegador.");

            if (handshake.ParentProcessId != parentProcessId)
                throw new InvalidOperationException("O processo que pediu a atualização não confere com o combinado.");

            UpdateApplyPolicy.EnsureInstallDirectoryMatches(installDirectory, handshake.InstallDirectory);
            UpdateApplyPolicy.EnsurePackageInsideUpdateRoot(packagePath);
            var helperExecutable = Environment.ProcessPath
                ?? throw new InvalidOperationException("Não foi possível identificar o processo auxiliar.");
            UpdateApplyPolicy.EnsureHelperLocation(helperExecutable, installDirectory);
            EnsureParentProcessMatchesInstallDirectory(parentProcessId, installDirectory);
            SignalHelperReady(updateDirectory, token);
            ApplyUpdate(parentProcessId, packagePath, installDirectory, updateDirectory, handshake);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "A atualização não pôde ser concluída.\n\n" + exception.Message +
                "\n\nAbra o Navegador novamente e tente atualizar.",
                Branding.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        return true;
    }

    /// <summary>Remove pacotes de atualização antigos. Nunca lança.</summary>
    public static void CleanupOldUpdateCache()
    {
        UpdatePaths.CleanupOldUpdates(TimeSpan.FromDays(1));
    }

    private static void EnsureParentProcessMatchesInstallDirectory(int parentProcessId, string installDirectory)
    {
        try
        {
            using var process = Process.GetProcessById(parentProcessId);
            var executable = process.MainModule?.FileName
                ?? throw new InvalidOperationException("Não foi possível identificar o executável da instância principal.");
            UpdateApplyPolicy.EnsureExecutableBelongsToInstallDirectory(executable, installDirectory);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException("A atualização foi recusada: não foi possível confirmar a instância principal do Navegador.", exception);
        }
    }

    private static void SignalHelperReady(string updateDirectory, string token) =>
        File.WriteAllText(UpdatePaths.ReadyFile(updateDirectory), token);

    private static void WaitForHelperReady(string updateDirectory, string token, Process helperProcess)
    {
        var readyFile = UpdatePaths.ReadyFile(updateDirectory);
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (DateTime.UtcNow < deadline)
        {
            if (helperProcess.HasExited)
                throw new InvalidOperationException("O auxiliar de atualização encerrou antes de ficar pronto.");
            try
            {
                if (File.Exists(readyFile) && string.Equals(File.ReadAllText(readyFile).Trim(), token, StringComparison.Ordinal))
                    return;
            }
            catch (IOException) { }
            Thread.Sleep(80);
        }
        throw new TimeoutException("O auxiliar de atualização não respondeu a tempo.");
    }

    private static void WriteHandshake(string updateDirectory, UpdateHandshake handshake)
    {
        // Fica em %LOCALAPPDATA%\Navegador\Updates, que já é uma pasta privada do
        // usuário. A defesa principal continua sendo hash + pasta de destino fixa.
        JsonFileStore.Write(UpdatePaths.MarkerFile(updateDirectory), handshake);
    }

    private static UpdateHandshake ReadHandshake(string updateDirectory)
    {
        var path = UpdatePaths.MarkerFile(updateDirectory);

        if (!File.Exists(path))
            throw new InvalidOperationException("O combinado da atualização não foi encontrado. Inicie a atualização pelo Navegador.");

        var handshake = JsonFileStore.Read<UpdateHandshake?>(path, static () => null);
        return handshake ?? throw new InvalidOperationException("O combinado da atualização está ilegível.");
    }

    private static async Task<ReleaseInfo> GetLatestReleaseAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApi);
        request.Headers.UserAgent.ParseAdd("Navegador-Windows");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return ReleaseParser.Parse(json);
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

    private static async Task<string> ValidatePackageAsync(
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

            expected = ReleaseParser.ParseChecksumFile(await response.Content.ReadAsStringAsync());
        }

        if (string.IsNullOrWhiteSpace(expected))
            throw new InvalidOperationException("A release não possui checksum SHA-256 para validação.");

        await UpdateApplyPolicy.EnsurePackageHashAsync(packagePath, expected);
        return expected;
    }

    private static void ApplyUpdate(
        int parentProcessId,
        string packagePath,
        string installDirectory,
        string updateDirectory,
        UpdateHandshake handshake)
    {
        WaitForBrowserToExit(parentProcessId);

        if (!File.Exists(packagePath))
            throw new FileNotFoundException("O pacote de atualização não foi encontrado.", packagePath);

        // O hash é conferido de novo aqui: o auxiliar não aceita o pacote só
        // porque alguém o colocou na pasta de atualizações.
        UpdateApplyPolicy.EnsurePackageHashAsync(packagePath, handshake.PackageSha256)
            .GetAwaiter()
            .GetResult();

        var stagingDirectory = UpdatePaths.StagingDirectory(updateDirectory);
        if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, recursive: true);
        Directory.CreateDirectory(stagingDirectory);

        ZipFile.ExtractToDirectory(packagePath, stagingDirectory, overwriteFiles: true);

        var stagedExecutable = Path.Combine(stagingDirectory, "Navegador.exe");

        if (!File.Exists(stagedExecutable))
            throw new InvalidOperationException("O pacote baixado não contém Navegador.exe.");

        if (!UpdateApplyPolicy.LooksLikeWindowsExecutable(stagedExecutable))
            throw new InvalidOperationException("O Navegador.exe do pacote não parece um executável válido do Windows.");

        var backupDirectory = UpdatePaths.BackupDirectory(updateDirectory);
        var createdFiles = new List<string>();

        try
        {
            foreach (var sourceFile in Directory.EnumerateFiles(stagingDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(stagingDirectory, sourceFile);

                // O perfil do usuário nunca é substituído por um pacote.
                if (IsProfilePath(relativePath)) continue;

                var destinationFile = Path.Combine(installDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

                var existed = File.Exists(destinationFile);
                Backup(destinationFile, installDirectory, backupDirectory);
                CopyWithRetry(sourceFile, destinationFile);
                if (!existed) createdFiles.Add(destinationFile);
            }
        }
        catch
        {
            // Nada de deixar a instalação pela metade: qualquer falha de cópia
            // devolve os arquivos anteriores.
            Restore(installDirectory, backupDirectory);
            foreach (var createdFile in createdFiles)
            {
                try { if (File.Exists(createdFile)) File.Delete(createdFile); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
            }
            throw;
        }

        var installedExecutable = Path.Combine(installDirectory, "Navegador.exe");
        if (!File.Exists(installedExecutable))
            throw new InvalidOperationException("Navegador.exe não foi encontrado após a atualização.");

        DeleteHandshake(updateDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = installedExecutable,
            WorkingDirectory = installDirectory,
            UseShellExecute = true
        });
    }

    private static bool IsProfilePath(string relativePath)
    {
        const string profileFolder = "Data";

        return relativePath.Equals(profileFolder, StringComparison.OrdinalIgnoreCase) ||
               relativePath.StartsWith(profileFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static void Backup(string destinationFile, string installDirectory, string backupDirectory)
    {
        if (!File.Exists(destinationFile)) return;

        var relativePath = Path.GetRelativePath(installDirectory, destinationFile);
        var backupFile = Path.Combine(backupDirectory, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(backupFile)!);
        File.Copy(destinationFile, backupFile, overwrite: true);
    }

    private static void Restore(string installDirectory, string backupDirectory)
    {
        try
        {
            if (!Directory.Exists(backupDirectory)) return;

            foreach (var backupFile in Directory.EnumerateFiles(backupDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(backupDirectory, backupFile);
                var destinationFile = Path.Combine(installDirectory, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                CopyWithRetry(backupFile, destinationFile);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Se nem a restauração funcionar, o usuário ainda tem o pacote baixado.
        }
    }

    private static void DeleteHandshake(string updateDirectory)
    {
        foreach (var path in new[] { UpdatePaths.MarkerFile(updateDirectory), UpdatePaths.ReadyFile(updateDirectory) })
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
        }
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
}
