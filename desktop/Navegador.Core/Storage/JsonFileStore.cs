using System.Text.Json;

namespace Navegador.Core.Storage;

/// <summary>
/// Leitura e escrita de JSON com gravação atômica.
///
/// A gravação usa um arquivo temporário seguido de <see cref="File.Move(string, string, bool)"/>:
/// se o navegador morrer no meio do salvamento, o arquivo bom anterior continua
/// intacto em vez de virar JSON truncado.
/// </summary>
public static class JsonFileStore
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Lê o arquivo; qualquer problema devolve <paramref name="fallback"/>.</summary>
    public static T Read<T>(string path, Func<T> fallback)
    {
        try
        {
            if (!File.Exists(path)) return fallback();

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return fallback();

            return JsonSerializer.Deserialize<T>(json, Options) ?? fallback();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return fallback();
        }
    }

    /// <summary>Grava o arquivo de forma atômica. Nunca lança.</summary>
    public static bool Write<T>(string path, T value)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, Options));
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }
}

