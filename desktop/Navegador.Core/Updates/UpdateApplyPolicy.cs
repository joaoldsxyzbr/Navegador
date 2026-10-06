using System.Security.Cryptography;

namespace Navegador.Core.Updates;

/// <summary>
/// Verificações que o processo <b>auxiliar</b> refaz por conta própria antes de
/// tocar em qualquer arquivo instalado.
///
/// O auxiliar é iniciado como uma nova instância de <c>Navegador.exe</c> com
/// argumentos na linha de comando. Se ele confiasse nesses argumentos, qualquer
/// processo do usuário poderia pedir para o Navegador descompactar um pacote
/// forjado por cima de qualquer pasta. Por isso: token combinado com o processo
/// pai, diretório de instalação fixo e hash conferido de novo aqui.
/// </summary>
public static class UpdateApplyPolicy
{
    public static string RequireInstallDirectory()
    {
        var expected = Path.GetFullPath(AppPaths.BaseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return expected + Path.DirectorySeparatorChar;
    }

    public static string NormalizeInstallDirectory(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            throw new InvalidOperationException("O diretório de instalação da atualização não foi informado.");

        var requested = Path.GetFullPath(candidate)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return requested + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// O auxiliar só pode substituir arquivos na pasta de onde ele mesmo está
    /// rodando. Qualquer outro destino é recusado.
    /// </summary>
    public static void EnsureInstallDirectoryMatches(string candidate)
    {
        var requested = NormalizeInstallDirectory(candidate);
        var expected = RequireInstallDirectory();

        if (!requested.Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A atualização foi recusada: o diretório de destino não é a pasta desta instalação.");
        }
    }

    public static void EnsurePackageInsideUpdateRoot(string packagePath)
    {
        if (!UpdatePaths.IsInsideRoot(packagePath))
        {
            throw new InvalidOperationException(
                "A atualização foi recusada: o pacote não está na pasta de atualizações do Navegador.");
        }
    }

    public static async Task EnsurePackageHashAsync(
        string packagePath,
        string? expectedSha256,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256))
            throw new InvalidOperationException("A atualização foi recusada: o pacote não tem hash SHA-256 para conferência.");

        await using var stream = File.OpenRead(packagePath);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));

        if (!actual.Equals(expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A atualização foi recusada: o pacote não corresponde ao hash esperado.");
    }

    /// <summary>
    /// O auxiliar precisa estar fora da pasta de instalação. Rodando de dentro
    /// dela, o próprio arquivo em uso seria sobrescrito no meio da cópia.
    /// </summary>
    public static void EnsureHelperOutsideInstallDirectory(string installDirectory)
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable))
            throw new InvalidOperationException("Não foi possível identificar o processo auxiliar da atualização.");

        var helperDirectory = Path.GetFullPath(Path.GetDirectoryName(executable)!)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        var install = NormalizeInstallDirectory(installDirectory);
        if (helperDirectory.Equals(install, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A atualização foi recusada: o auxiliar precisa rodar fora da pasta de instalação.");
        }
    }

    /// <summary>Confere que o executável extraído é um binário PE do Windows.</summary>
    public static bool LooksLikeWindowsExecutable(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> signature = stackalloc byte[2];
            if (stream.Read(signature) != 2) return false;

            return signature[0] == (byte)'M' && signature[1] == (byte)'Z';
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
