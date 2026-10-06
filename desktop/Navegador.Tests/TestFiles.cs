namespace Navegador.Tests;

/// <summary>Pasta temporária para um caso de teste, sempre removida no fim.</summary>
internal static class TestFiles
{
    public static string CreateDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Navegador.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);
        return directory;
    }

    public static void Delete(string directory)
    {
        try
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Pasta temporária presa não deve falhar o teste.
        }
    }
}
