using System.Text.Json;
using Microsoft.Maui.Storage;

namespace Navegador.Services;

public sealed record Bookmark(string Url, string Title);
public sealed record HistoryEntry(string Url, string Title, DateTimeOffset VisitedAt);

public sealed class BrowserDataStore
{
    private const string BookmarksFileName = "favoritos.json";
    private const string HistoryFileName = "historico.json";
    private const int MaxBookmarks = 200;
    private const int MaxHistoryEntries = 500;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<IReadOnlyList<Bookmark>> GetBookmarksAsync()
    {
        await _gate.WaitAsync();

        try
        {
            return await ReadListAsync<Bookmark>(BookmarksFileName);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> IsBookmarkedAsync(string url)
    {
        var bookmarks = await GetBookmarksAsync();
        return bookmarks.Any(item => string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> ToggleBookmarkAsync(string url, string title)
    {
        await _gate.WaitAsync();

        try
        {
            var bookmarks = await ReadListAsync<Bookmark>(BookmarksFileName);
            var existing = bookmarks.FindIndex(item =>
                string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase));

            if (existing >= 0)
            {
                bookmarks.RemoveAt(existing);
                await WriteListAsync(BookmarksFileName, bookmarks);
                return false;
            }

            bookmarks.Insert(0, new Bookmark(url, title));

            if (bookmarks.Count > MaxBookmarks)
                bookmarks.RemoveRange(MaxBookmarks, bookmarks.Count - MaxBookmarks);

            await WriteListAsync(BookmarksFileName, bookmarks);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<HistoryEntry>> GetHistoryAsync()
    {
        await _gate.WaitAsync();

        try
        {
            return await ReadListAsync<HistoryEntry>(HistoryFileName);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddHistoryAsync(string url, string title)
    {
        await _gate.WaitAsync();

        try
        {
            var history = await ReadListAsync<HistoryEntry>(HistoryFileName);

            if (history.Count > 0 &&
                string.Equals(history[0].Url, url, StringComparison.OrdinalIgnoreCase))
            {
                history[0] = history[0] with
                {
                    Title = title,
                    VisitedAt = DateTimeOffset.UtcNow
                };
            }
            else
            {
                history.Insert(0, new HistoryEntry(url, title, DateTimeOffset.UtcNow));
            }

            if (history.Count > MaxHistoryEntries)
                history.RemoveRange(MaxHistoryEntries, history.Count - MaxHistoryEntries);

            await WriteListAsync(HistoryFileName, history);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<T>> ReadListAsync<T>(string fileName)
    {
        var path = GetPath(fileName);

        if (!File.Exists(path))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private async Task WriteListAsync<T>(string fileName, List<T> items)
    {
        var path = GetPath(fileName);
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
    }
}
