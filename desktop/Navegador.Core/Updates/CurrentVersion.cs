using System.Reflection;

namespace Navegador.Core.Updates;

/// <summary>Versão instalada, lida do assembly em execução.</summary>
public static class CurrentVersion
{
    public static Version Value =>
        Assembly.GetEntryAssembly()?.GetName().Version
        ?? Assembly.GetExecutingAssembly().GetName().Version
        ?? new Version(0, 0, 0, 0);

    public static string Display => VersionFormatter.ToDisplay(Value);
}
