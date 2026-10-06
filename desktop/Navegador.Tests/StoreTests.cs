using Navegador.Core.Storage;
using Navegador.Core.Models;

namespace Navegador.Tests;

public static class StoreTests
{
    public static IEnumerable<TestCase> Cases =>
    [
        new("favoritos: salvar e recarregar", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "favorites.json");
                var store = FavoritesStore.Load(path);

                Assert.True(store.Add("https://exemplo.com", "Exemplo"));
                Assert.Equal(1, store.Count);

                var reloaded = FavoritesStore.Load(path);
                Assert.Equal(1, reloaded.Count);
                Assert.Equal("Exemplo", reloaded.Items[0].Title);
                Assert.True(reloaded.Contains("https://exemplo.com"));
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("favoritos: não duplica e atualiza o título", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "favorites.json");
                var store = FavoritesStore.Load(path);

                Assert.True(store.Add("https://exemplo.com", "Antigo"));
                Assert.False(store.Add("https://exemplo.com", "Novo"));

                Assert.Equal(1, store.Count);
                Assert.Equal("Novo", store.Items[0].Title);
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("favoritos: recusa URL não navegável", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var store = FavoritesStore.Load(Path.Combine(directory, "favorites.json"));

                Assert.False(store.Add("javascript:alert(1)", "Ruim"));
                Assert.Equal(0, store.Count);
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("favoritos: remove e alterna", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var store = FavoritesStore.Load(Path.Combine(directory, "favorites.json"));

                store.Add("https://a.com", "A");
                Assert.False(store.Toggle("https://a.com", "A"));
                Assert.Equal(0, store.Count);
                Assert.True(store.Toggle("https://a.com", "A"));
                Assert.Equal(1, store.Count);
                Assert.True(store.Remove("https://a.com"));
                Assert.Equal(0, store.Count);
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("histórico: visita repetida sobe para o topo e conta", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "history.json");
                var store = HistoryStore.Load(path);
                var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

                store.Record("https://a.com", "A", baseTime);
                store.Record("https://b.com", "B", baseTime.AddMinutes(1));
                store.Record("https://a.com", "A atualizado", baseTime.AddMinutes(2));
                store.Save();

                Assert.Equal(2, store.Count);
                Assert.Equal("https://a.com", store.Items[0].Url);
                Assert.Equal(2, store.Items[0].VisitCount);
                Assert.Equal("A atualizado", store.Items[0].Title);

                var reloaded = HistoryStore.Load(path);
                Assert.Equal(2, reloaded.Count);
                Assert.Equal("https://a.com", reloaded.Items[0].Url);
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("histórico: busca por termos em título e URL", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var store = HistoryStore.Load(Path.Combine(directory, "history.json"));
                store.Record("https://exemplo.com/docs", "Documentação");
                store.Record("https://outro.com", "Outra coisa");

                Assert.Single(store.Search("documentação"));
                Assert.Single(store.Search("exemplo"));
                Assert.Equal(2, store.Search(null).Count());
                Assert.Empty(store.Search("inexistente"));
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("histórico: atualizar título não conta nova visita", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var store = HistoryStore.Load(Path.Combine(directory, "history.json"));
                store.Record("https://exemplo.com", "Título inicial");
                Assert.True(store.UpdateTitle("https://exemplo.com", "Título final"));
                Assert.Equal(1, store.Items[0].VisitCount);
                Assert.Equal("Título final", store.Items[0].Title);
                Assert.False(store.UpdateTitle("https://exemplo.com", "Título final"));
            }
            finally { TestFiles.Delete(directory); }
        }),

        new("sessão: salva, recarrega e descarta URL inválida", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "session.json");
                var snapshot = new SessionSnapshot
                {
                    Tabs =
                    [
                        new SessionTab { Url = "https://a.com", Title = "A" },
                        new SessionTab { Url = "javascript:alert(1)", Title = "Ruim" },
                        new SessionTab { Url = "https://b.com", Title = "B" }
                    ],
                    ActiveIndex = 2
                };

                Assert.True(SessionStore.Save(snapshot, path));

                var loaded = SessionStore.Load(path);
                Assert.Equal(2, loaded.Tabs.Count);
                Assert.Equal("https://a.com", loaded.Tabs[0].Url);
                Assert.Equal("https://b.com", loaded.Tabs[1].Url);
                // A aba ativa era a última; ao descartar a inválida ela virou a segunda.
                Assert.Equal(1, loaded.ActiveIndex);
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("sessão: aba ativa inválida passa para a anterior", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "session.json");
                var snapshot = new SessionSnapshot
                {
                    Tabs =
                    [
                        new SessionTab { Url = "https://a.com", Title = "A" },
                        new SessionTab { Url = "https://b.com", Title = "B" },
                        new SessionTab { Url = "javascript:alert(1)", Title = "Ruim" }
                    ],
                    ActiveIndex = 2
                };

                Assert.True(SessionStore.Save(snapshot, path));

                var loaded = SessionStore.Load(path);
                Assert.Equal(2, loaded.Tabs.Count);
                Assert.Equal(1, loaded.ActiveIndex);
                Assert.Equal("https://b.com", loaded.Tabs[loaded.ActiveIndex].Url);
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("sessão: todas as abas inválidas resultam em lista vazia", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "session.json");
                var snapshot = new SessionSnapshot
                {
                    Tabs = [new SessionTab { Url = "javascript:alert(1)" }],
                    ActiveIndex = 0
                };

                Assert.True(SessionStore.Save(snapshot, path));

                var loaded = SessionStore.Load(path);
                Assert.Empty(loaded.Tabs);
                Assert.Equal(0, loaded.ActiveIndex);
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("sessão: índice ativo fora da faixa é corrigido", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "session.json");
                SessionStore.Save(new SessionSnapshot
                {
                    Tabs = [new SessionTab { Url = "https://a.com" }],
                    ActiveIndex = 7
                }, path);

                Assert.Equal(0, SessionStore.Load(path).ActiveIndex);
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("sessão: arquivo ausente devolve sessão vazia", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var snapshot = SessionStore.Load(Path.Combine(directory, "nao-existe.json"));
                Assert.Empty(snapshot.Tabs);
                Assert.Equal(0, snapshot.ActiveIndex);
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("JSON corrompido não derruba o navegador", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "favorites.json");
                File.WriteAllText(path, "{ isso não é json");

                var store = FavoritesStore.Load(path);
                Assert.Equal(0, store.Count);
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("gravação é atômica: não deixa arquivo temporário", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "favorites.json");
                var store = FavoritesStore.Load(path);
                store.Add("https://exemplo.com", "Exemplo");

                Assert.True(File.Exists(path));
                Assert.False(File.Exists(path + ".tmp"));
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("configurações: padrões e pasta de downloads", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "settings.json");
                var store = SettingsStore.Load(path);

                Assert.True(store.Current.RestoreSession);
                Assert.EndsWith("Downloads", store.ResolveDownloadFolder());

                store.Current.DownloadFolder = @"C:\Saidas";
                Assert.True(store.Save());

                var reloaded = SettingsStore.Load(path);
                Assert.Equal(@"C:\Saidas", reloaded.Current.DownloadFolder);
                Assert.Equal(@"C:\Saidas", reloaded.ResolveDownloadFolder());
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        })
    ];
}
