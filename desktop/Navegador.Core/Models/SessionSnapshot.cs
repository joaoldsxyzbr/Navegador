namespace Navegador.Core.Models;

/// <summary>
/// Sessão salva ao fechar o navegador. Só existe uma aba por URL nesta lista e a
/// ordem é a ordem visual das abas.
/// </summary>
public sealed class SessionSnapshot
{
    public List<SessionTab> Tabs { get; set; } = [];

    public int ActiveIndex { get; set; }

    public static SessionSnapshot Empty => new();
}
