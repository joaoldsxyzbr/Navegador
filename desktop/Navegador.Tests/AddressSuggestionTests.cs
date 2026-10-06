using Navegador.Core;
using Navegador.Core.Models;

namespace Navegador.Tests;

public static class AddressSuggestionTests
{
    public static IEnumerable<TestCase> Cases =>
    [
        new("omnibox: favoritos e histórico são encontrados por título ou URL", () =>
        {
            var favorites = new[]
            {
                new Favorite { Title = "Documentação Rumo", Url = "https://docs.exemplo.com" }
            };
            var history = new[]
            {
                new HistoryEntry { Title = "Notícias", Url = "https://jornal.exemplo.com" }
            };

            var suggestions = AddressSuggestionResolver.Suggest("docs", favorites, history);

            Assert.Equal(1, suggestions.Count);
            Assert.Equal("https://docs.exemplo.com", suggestions[0].Url);
            Assert.True(suggestions[0].IsFavorite);
            var historySuggestion = AddressSuggestionResolver.Suggest("Notícias", [], history);
            Assert.Equal("https://jornal.exemplo.com", historySuggestion[0].Url);
        }),

        new("omnibox: favoritos aparecem antes do histórico e URLs duplicadas não repetem", () =>
        {
            var favorites = new[]
            {
                new Favorite { Title = "GitHub", Url = "https://github.com" }
            };
            var history = new[]
            {
                new HistoryEntry { Title = "GitHub visitado", Url = "https://github.com" },
                new HistoryEntry { Title = "GitHub Docs", Url = "https://docs.github.com" }
            };

            var suggestions = AddressSuggestionResolver.Suggest("github", favorites, history);

            Assert.Equal(2, suggestions.Count);
            Assert.True(suggestions[0].IsFavorite);
            Assert.Equal("https://docs.github.com", suggestions[1].Url);
        }),

        new("omnibox: respeita limite e ignora consulta vazia", () =>
        {
            var history = Enumerable.Range(1, 4)
                .Select(index => new HistoryEntry { Title = "Site", Url = $"https://site{index}.com" })
                .ToArray();

            Assert.Equal(2, AddressSuggestionResolver.Suggest("site", [], history, maximum: 2).Count);
            Assert.Equal(0, AddressSuggestionResolver.Suggest("  ", [], history).Count);
        })
    ];
}
