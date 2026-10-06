namespace Navegador.Core.Models;

/// <summary>Uma aba que existia quando o navegador foi fechado.</summary>
public sealed class SessionTab
{
    public string Url { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}
