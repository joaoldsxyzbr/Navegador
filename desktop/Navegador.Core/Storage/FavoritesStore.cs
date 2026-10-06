using Navegador.Core.Models;

namespace Navegador.Core.Storage;

/// <summary>Favoritos do usuário, persistidos em <c>Data/favorites.json</c>.</summary>
public sealed class FavoritesStore
{
    private const int MaxFavorites = 2000;

    private readonly List<Favorite> _items;

    private FavoritesStore(List<Favorite> items, string path)
    {
        _items = items;
        Path = path;
    }

    public static FavoritesStore Load(string? path = null)
    {
        var file = path ?? AppPaths.FavoritesFile;
        var items = JsonFileStore.Read(file, static () => new List<Favorite>());

        // Nunca confie no arquivo: ele pode ter sido editado à mão.
        items.RemoveAll(item => !AddressResolver.IsPersistable(item.Url));
        return new FavoritesStore(items, file);
    }

    public string Path { get; }

    public IReadOnlyList<Favorite> Items => _items;

    public int Count => _items.Count;

    public bool Contains(string? url) =>
        url is not null && _items.Any(item => SameUrl(item.Url, url));

    public Favorite? Find(string? url) =>
        url is null ? null : _items.FirstOrDefault(item => SameUrl(item.Url, url));

    public bool Add(string url, string? title)
    {
        if (!AddressResolver.IsPersistable(url)) return false;

        var existing = Find(url);
        if (existing is not null)
        {
            // Salvar de novo atualiza o título em vez de duplicar.
            existing.Title = string.IsNullOrWhiteSpace(title) ? existing.Title : title;
            Save();
            return false;
        }

        _items.Insert(0, Favorite.Create(url, title));
        if (_items.Count > MaxFavorites) _items.RemoveRange(MaxFavorites, _items.Count - MaxFavorites);

        Save();
        return true;
    }

    public bool Remove(string? url)
    {
        if (url is null) return false;

        var removed = _items.RemoveAll(item => SameUrl(item.Url, url)) > 0;
        if (removed) Save();
        return removed;
    }

    public bool Toggle(string url, string? title) => Contains(url) ? !Remove(url) : Add(url, title);

    public IEnumerable<Favorite> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return _items;

        return _items.Where(item =>
            item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            item.Url.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    public void Clear()
    {
        _items.Clear();
        Save();
    }

    public void Save() => JsonFileStore.Write(Path, _items);

    private static bool SameUrl(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
