using Navegador.Core.Models;

namespace Navegador.Core.Storage;

/// <summary>Preferências do usuário, persistidas em <c>Data/settings.json</c>.</summary>
public sealed class SettingsStore
{
    private SettingsStore(BrowserSettings settings, string path)
    {
        Current = settings;
        Path = path;
    }

    public static SettingsStore Load(string? path = null)
    {
        var file = path ?? AppPaths.SettingsFile;
        return new SettingsStore(JsonFileStore.Read(file, static () => new BrowserSettings()), file);
    }

    public string Path { get; }

    public BrowserSettings Current { get; }

    /// <summary>Pasta efetiva dos downloads, resolvendo o padrão do Windows.</summary>
    public string ResolveDownloadFolder()
    {
        if (!string.IsNullOrWhiteSpace(Current.DownloadFolder)) return Current.DownloadFolder;

        return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
    }

    public bool Save() => JsonFileStore.Write(Path, Current);
}
