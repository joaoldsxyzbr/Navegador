using Navegador.Core.Models;

namespace Navegador.Core;

/// <summary>Uma sugestão apresentada na barra de endereço.</summary>
public sealed record AddressSuggestion(string Title, string Url, bool IsFavorite);

/// <summary>Combina favoritos e histórico para sugerir destinos enquanto se digita.</summary>
public static class AddressSuggestionResolver
{
    public static IReadOnlyList<AddressSuggestion> Suggest(
        string? query,
        IEnumerable<Favorite> favorites,
        IEnumerable<HistoryEntry> history,
        int maximum = 8)
    {
        if (string.IsNullOrWhiteSpace(query) || maximum <= 0) return [];

        var term = query.Trim();
        var results = new List<AddressSuggestion>(maximum);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var favorite in favorites)
        {
            if (Matches(favorite.Title, favorite.Url, term) && seen.Add(favorite.Url))
            {
                results.Add(new AddressSuggestion(favorite.Title, favorite.Url, IsFavorite: true));
                if (results.Count >= maximum) return results;
            }
        }

        foreach (var entry in history)
        {
            if (Matches(entry.Title, entry.Url, term) && seen.Add(entry.Url))
            {
                results.Add(new AddressSuggestion(entry.Title, entry.Url, IsFavorite: false));
                if (results.Count >= maximum) break;
            }
        }

        return results;
    }

    private static bool Matches(string title, string url, string query) =>
        title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        url.Contains(query, StringComparison.OrdinalIgnoreCase);
}
