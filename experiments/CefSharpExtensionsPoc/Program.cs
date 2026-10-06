using CefSharp;
using CefSharp.WinForms;

namespace Rumo.CefSharpPoc;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // CEF usa o mesmo executável para os subprocessos de renderização,
        // GPU e utilitários. Eles encerram aqui sem abrir outra janela.
        var subprocessExitCode = CefSharp.BrowserSubprocess.SelfHost.Main(args);
        if (subprocessExitCode >= 0) return subprocessExitCode;

        ApplicationConfiguration.Initialize();

        var profileRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rumo",
            "CefSharpPoc");
        Directory.CreateDirectory(profileRoot);

        // RuntimeStyle precisa ser escolhido antes de inicializar o CEF.
        CefSharpSettings.RuntimeStyle = CefRuntimeStyle.Chrome;
        CefSharpSettings.ShutdownOnExit = false;

        var settings = new CefSettings
        {
            BrowserSubprocessPath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Não foi possível localizar o executável da PoC."),
            RootCachePath = profileRoot,
            CachePath = Path.Combine(profileRoot, "Default")
        };

        var initialized = false;
        try
        {
            initialized = Cef.Initialize(settings);
            if (!initialized)
            {
                MessageBox.Show(
                    "O CefSharp não conseguiu iniciar. Confira se o Visual C++ Redistributable x64 está instalado.",
                    "Rumo · PoC CefSharp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }

            Application.Run(new MainForm(profileRoot));
            return 0;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Falha ao iniciar a PoC do CefSharp:\n{exception.Message}",
                "Rumo · PoC CefSharp",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
        finally
        {
            if (initialized) Cef.Shutdown();
        }
    }
}
