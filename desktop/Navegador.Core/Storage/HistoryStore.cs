using Navegador.Core.Models;

namespace Navegador.Core.Storage;

/// <summary>Histórico de navegação, persistido em <c>Data/history.json</c>.</summary>
public sealed class HistoryStore
{
    private const int MaxEntries = 5000;

    private readonly List<HistoryEntry> _items;

    private HistoryStore(List<HistoryEntry> items, string path)
    {
        _items = items;
        Path = path;
    }

    public static HistoryStore Load(string? path = null)
    {
        var file = path ?? AppPaths.HistoryFile;
        var items = JsonFileStore.Read(file, static () => new List<HistoryEntry>());

        items.RemoveAll(item => !AddressResolver.IsPersistable(item.Url));
        items.Sort(static (left, right) => right.LastVisitedAt.CompareTo(left.LastVisitedAt));
        return new HistoryStore(items, file);
    }

    public string Path { get; }

    public IReadOnlyList<HistoryEntry> Items => _items;

    public int Count => _items.Count;

    /// <summary>Registra uma visita. A entrada mais recente fica no topo.</summary>
    public HistoryEntry Record(string url, string? title, DateTimeOffset? when = null)
    {
        var moment = when ?? DateTimeOffset.Now;
        var existing = _items.FirstOrDefault(item =>
            string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            _items.Remove(existing);
            existing.LastVisitedAt = moment;
            existing.VisitCount++;
            if (!string.IsNullOrWhiteSpace(title)) existing.Title = title;
            _items.Insert(0, existing);
            return existing;
        }

        var entry = new HistoryEntry
        {
            Url = url,
            Title = string.IsNullOrWhiteSpace(title) ? url : title,
            LastVisitedAt = moment
        };

        _items.Insert(0, entry);
        if (_items.Count > MaxEntries) _items.RemoveRange(MaxEntries, _items.Count - MaxEntries);
        return entry;
    }

    public bool UpdateTitle(string url, string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return false;
        var existing = _items.FirstOrDefault(item => string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase));
        if (existing is null || string.Equals(existing.Title, title, StringComparison.Ordinal)) return false;
        existing.Title = title;
        return true;
    }

    public IEnumerable<HistoryEntry> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return _items;

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return _items.Where(entry => terms.All(term =>
            entry.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            entry.Url.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    public bool Remove(string? url)
    {
        if (url is null) return false;

        var removed = _items.RemoveAll(item =>
            string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed) Save();
        return removed;
    }

    public void Clear()
    {
        _items.Clear();
        Save();
    }

    public bool Save() => JsonFileStore.Write(Path, _items);
}
