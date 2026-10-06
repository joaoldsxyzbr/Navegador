using System.Security.Cryptography;

namespace Navegador.Core.Updates;

public static class UpdateApplyPolicy
{
    public static string RequireInstallDirectory() => NormalizeInstallDirectory(AppPaths.BaseDirectory);

    public static string NormalizeInstallDirectory(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            throw new InvalidOperationException("O diretório de instalação da atualização não foi informado.");
        var requested = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return requested + Path.DirectorySeparatorChar;
    }

    public static void EnsureInstallDirectoryMatches(string candidate, string expectedDirectory)
    {
        var requested = NormalizeInstallDirectory(candidate);
        var expected = NormalizeInstallDirectory(expectedDirectory);
        if (!requested.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A atualização foi recusada: o diretório de destino não corresponde à instalação que iniciou a atualização.");
    }

    public static void EnsureExecutableBelongsToInstallDirectory(string executablePath, string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new InvalidOperationException("O executável da instância principal não pôde ser identificado.");
        var expected = Path.GetFullPath(Path.Combine(NormalizeInstallDirectory(installDirectory), "Navegador.exe"));
        var actual = Path.GetFullPath(executablePath);
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A atualização foi recusada: o processo principal não pertence à instalação de destino.");
    }

    public static void EnsurePackageInsideUpdateRoot(string packagePath)
    {
        if (!UpdatePaths.IsInsideRoot(packagePath))
            throw new InvalidOperationException("A atualização foi recusada: o pacote não está na pasta de atualizações do Navegador.");
    }

    public static void EnsureHelperLocation(string helperPath, string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(helperPath) || !UpdatePaths.IsInsideRoot(helperPath))
            throw new InvalidOperationException("A atualização foi recusada: o auxiliar não está na pasta de atualizações do Navegador.");
        var helper = Path.GetFullPath(helperPath);
        var install = NormalizeInstallDirectory(installDirectory);
        if (helper.StartsWith(install, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A atualização foi recusada: o auxiliar precisa rodar fora da pasta de instalação.");
    }

    public static async Task EnsurePackageHashAsync(string packagePath, string? expectedSha256, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256))
            throw new InvalidOperationException("A atualização foi recusada: o pacote não tem hash SHA-256 para conferência.");
        await using var stream = File.OpenRead(packagePath);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
        if (!actual.Equals(expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A atualização foi recusada: o pacote não corresponde ao hash esperado.");
    }

    public static bool LooksLikeWindowsExecutable(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> signature = stackalloc byte[2];
            return stream.Read(signature) == 2 && signature[0] == (byte)'M' && signature[1] == (byte)'Z';
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return false; }
    }
}
