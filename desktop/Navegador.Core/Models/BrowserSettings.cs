namespace Navegador.Core.Models;

/// <summary>Preferências do usuário persistidas em <c>Data/settings.json</c>.</summary>
public sealed class BrowserSettings
{
    /// <summary>Ao abrir, reabre as abas da sessão anterior.</summary>
    public bool RestoreSession { get; set; } = true;

    /// <summary>Pasta de destino dos downloads. Vazio significa "Downloads do usuário".</summary>
    public string DownloadFolder { get; set; } = string.Empty;

    /// <summary>Perguntar onde salvar antes de cada download.</summary>
    public bool AskWhereToSaveDownloads { get; set; }

    /// <summary>Endereço aberto quando não há sessão para restaurar.</summary>
    public string HomeUrl { get; set; } = "https://www.google.com/";
}
