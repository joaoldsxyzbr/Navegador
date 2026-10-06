namespace Navegador.Core.Updates;

/// <summary>
/// Caminhos usados pelo atualizador.
///
/// Tudo fica em <c>%LOCALAPPDATA%\Navegador\Updates</c> em vez de <c>%TEMP%</c>:
/// a pasta temporária do usuário é um ponto de escrita compartilhado e não é um
/// bom lugar para guardar um pacote que vai substituir o executável em uso.
/// </summary>
public static class UpdatePaths
{
    private const string MarkerFileName = "update.json";
    private const string ChecksumFileName = "package.sha256";
    private const string StagingFolderName = "staging";
    private const string BackupFolderName = "backup";
    private const string DownloadedPackageName = "package.zip";
    private const string HelperFileName = "Navegador.Atualizador.exe";
    private const string ReadyFileName = "helper.ready";

    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Navegador",
        "Updates");

    public static string CreateUpdateDirectory(string version)
    {
        var directory = Path.Combine(Root, $"{Sanitize(version)}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>Nome fixo: o pacote novo nunca convive com o antigo na mesma pasta.</summary>
    public static string PackageFile(string updateDirectory) =>
        Path.Combine(updateDirectory, DownloadedPackageName);

    public static string ChecksumFile(string updateDirectory) =>
        Path.Combine(updateDirectory, ChecksumFileName);

    public static string MarkerFile(string updateDirectory) =>
        Path.Combine(updateDirectory, MarkerFileName);

    public static string StagingDirectory(string updateDirectory) =>
        Path.Combine(updateDirectory, StagingFolderName);

    public static string BackupDirectory(string updateDirectory) => Path.Combine(updateDirectory, BackupFolderName);
    public static string HelperFile(string updateDirectory) => Path.Combine(updateDirectory, HelperFileName);
    public static string ReadyFile(string updateDirectory) => Path.Combine(updateDirectory, ReadyFileName);

    /// <summary>Verdadeiro quando o caminho está debaixo da raiz de atualizações.</summary>
    public static bool IsInsideRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        var full = Path.GetFullPath(path);
        var root = Path.GetFullPath(Root);
        return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Apaga pastas de atualização antigas. Nunca lança.</summary>
    public static void CleanupOldUpdates(TimeSpan olderThan, string? keep = null)
    {
        try
        {
            if (!Directory.Exists(Root)) return;

            var keepFull = keep is null ? null : Path.GetFullPath(keep);
            foreach (var directory in Directory.EnumerateDirectories(Root))
            {
                try
                {
                    if (keepFull is not null &&
                        Path.GetFullPath(directory).Equals(keepFull, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (Directory.GetCreationTimeUtc(directory) < DateTime.UtcNow - olderThan)
                        Directory.Delete(directory, recursive: true);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // Um helper ainda em execução pode manter a pasta bloqueada.
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Limpeza de cache nunca deve impedir o navegador de abrir.
        }
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
