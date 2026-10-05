using System.Diagnostics;
using System.Text;
using Microsoft.Maui.Controls;

namespace Navegador.Platforms.Windows;

internal static class UpdateInstaller
{
    public static Task<bool> InstallAsync(string packagePath)
    {
        var executablePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(executablePath) ||
            !File.Exists(packagePath) ||
            !string.Equals(Path.GetExtension(packagePath), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Não foi possível localizar o aplicativo ou o pacote.");
        }

        var installDirectory = AppContext.BaseDirectory;
        var scriptPath = Path.Combine(Path.GetTempPath(), $"navegador-update-{Guid.NewGuid():N}.ps1");
        var script = $$"""
            $ErrorActionPreference = 'Stop'
            $installDirectory = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{Encode(installDirectory)}}'))
            $packagePath = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{Encode(packagePath)}}'))
            $executablePath = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{Encode(executablePath)}}'))
            $processId = {{Environment.ProcessId}}
            while (Get-Process -Id $processId -ErrorAction SilentlyContinue) {
                Start-Sleep -Milliseconds 500
            }
            Expand-Archive -LiteralPath $packagePath -DestinationPath $installDirectory -Force
            Start-Process -FilePath $executablePath
            Remove-Item -LiteralPath $packagePath -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue
            """;

        File.WriteAllText(scriptPath, script, new UTF8Encoding(false));
        Process.Start(new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = true,
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\""
        });

        Application.Current?.Quit();
        return Task.FromResult(true);
    }

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}
