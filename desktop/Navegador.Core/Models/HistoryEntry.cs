namespace Navegador.Core.Models;

/// <summary>Uma visita registrada no histórico.</summary>
public sealed class HistoryEntry
{
    public string Url { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset LastVisitedAt { get; set; } = DateTimeOffset.Now;

    public int VisitCount { get; set; } = 1;
}
