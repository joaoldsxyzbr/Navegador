using Navegador.Core.Models;

namespace Navegador.Core.Storage;

/// <summary>Sessão de abas, persistida em <c>Data/session.json</c>.</summary>
public static class SessionStore
{
    public static SessionSnapshot Load(string? path = null)
    {
        var snapshot = JsonFileStore.Read(path ?? AppPaths.SessionFile, static () => SessionSnapshot.Empty);

        snapshot.Tabs.RemoveAll(tab => !AddressResolver.IsPersistable(tab.Url));
        if (snapshot.ActiveIndex < 0 || snapshot.ActiveIndex >= snapshot.Tabs.Count)
            snapshot.ActiveIndex = 0;

        return snapshot;
    }

    public static bool Save(SessionSnapshot snapshot, string? path = null)
    {
        // A aba ativa é identificada por posição: comparar índices antigos com a
        // lista já filtrada erra por um. Se a própria aba ativa for descartada,
        // ativamos a vizinha que ocupou o lugar dela — como o Chrome faz.
        var tabs = snapshot.Tabs;
        var activeIndex = Math.Clamp(snapshot.ActiveIndex, 0, Math.Max(tabs.Count - 1, 0));

        var kept = new List<SessionTab>(tabs.Count);
        var validBeforeActive = 0;

        for (var index = 0; index < tabs.Count; index++)
        {
            if (!AddressResolver.IsPersistable(tabs[index].Url)) continue;

            if (index < activeIndex) validBeforeActive++;
            kept.Add(tabs[index]);
        }

        snapshot.Tabs = kept;
        snapshot.ActiveIndex = kept.Count == 0
            ? 0
            : Math.Clamp(validBeforeActive, 0, kept.Count - 1);

        return JsonFileStore.Write(path ?? AppPaths.SessionFile, snapshot);
    }

    public static void Clear(string? path = null)
    {
        var file = path ?? AppPaths.SessionFile;
        try
        {
            if (File.Exists(file)) File.Delete(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Apagar a sessão é melhor esforço.
        }
    }
}
