namespace Navegador.Core.Updates;

/// <summary>Dados combinados entre o processo pai e o auxiliar da atualização.</summary>
public sealed class UpdateHandshake
{
    public string Token { get; set; } = string.Empty;

    public int ParentProcessId { get; set; }

    public string InstallDirectory { get; set; } = string.Empty;

    public string PackageSha256 { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public static UpdateHandshake Create(int parentProcessId, string installDirectory, string packageSha256, string version) => new()
    {
        Token = Guid.NewGuid().ToString("N"),
        ParentProcessId = parentProcessId,
        InstallDirectory = installDirectory,
        PackageSha256 = packageSha256,
        Version = version
    };

    public bool MatchesToken(string? candidate) =>
        !string.IsNullOrWhiteSpace(Token) &&
        !string.IsNullOrWhiteSpace(candidate) &&
        string.Equals(Token, candidate, StringComparison.Ordinal);
}
