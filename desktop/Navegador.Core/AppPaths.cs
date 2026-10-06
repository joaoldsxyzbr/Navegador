namespace Navegador.Core;

/// <summary>
/// Diretórios de dados do Navegador.
///
/// O app é portátil: os dados ficam em <c>Data</c> ao lado do executável.
/// Quando essa pasta não pode ser criada (por exemplo, o app foi extraído em
/// uma pasta somente leitura ou em Program Files sem elevação), caímos para
/// <c>%LOCALAPPDATA%\Navegador</c> em vez de falhar na inicialização.
/// </summary>
public static class AppPaths
{
    private const string PortableFolderName = "Data";
    private const string LocalFolderName = "Navegador";

    private static readonly Lazy<string> ResolvedDataDirectory = new(ResolveDataDirectory);
    private static readonly Lazy<bool> ResolvedPortable = new(() => IsUsable(BaseDirectory));

    /// <summary>Pasta onde o executável está.</summary>
    public static string BaseDirectory => AppContext.BaseDirectory;

    /// <summary>Verdadeiro quando os dados ficam ao lado do executável.</summary>
    public static bool IsPortable => ResolvedPortable.Value;

    /// <summary>Pasta raiz dos dados (sessão, favoritos, histórico, perfil).</summary>
    public static string DataDirectory => ResolvedDataDirectory.Value;

    public static string WebViewProfileDirectory => Path.Combine(DataDirectory, "WebView2");

    public static string SessionFile => Path.Combine(DataDirectory, "session.json");

    public static string FavoritesFile => Path.Combine(DataDirectory, "favorites.json");

    public static string HistoryFile => Path.Combine(DataDirectory, "history.json");

    public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    /// <summary>Caminho do arquivo <c>Navegador.exe</c> em uso.</summary>
    public static string CurrentExecutable => Path.Combine(BaseDirectory, "Navegador.exe");

    public static void EnsureDataDirectory()
    {
        Directory.CreateDirectory(DataDirectory);
    }

    private static string ResolveDataDirectory()
    {
        // Dentro de um único processo a decisão precisa ser estável: se o modo
        // portátil funciona, tudo vai para Data; senão, tudo vai para LOCALAPPDATA.
        var portable = Path.Combine(BaseDirectory, PortableFolderName);
        if (IsUsable(portable)) return portable;

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LocalFolderName);

        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static bool IsUsable(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);

            // Gravar de verdade: CreateDirectory não falha em pasta somente leitura.
            var probe = Path.Combine(directory, ".write-test");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }
}
