using Navegador.Core;
using Navegador.Core.Updates;
using Navegador.Windows.Ui;
using CefSharp;
using CefSharp.WinForms;

namespace Navegador.Windows;

internal static class Program
{
    private const string InstanceMutexName = @"Local\Navegador.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Segunda fase da atualização: este processo é o auxiliar que troca os
        // arquivos e reinicia o Navegador.
        if (UpdateService.TryRunApplyUpdate(args)) return;

        // Instância única: duas cópias escreveriam a mesma sessão e o mesmo perfil.
        using var instance = TryAcquireSingleInstance();

        if (instance is null)
        {
            MessageBox.Show(
                $"O {Branding.Name} já está aberto.",
                Branding.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        AppPaths.EnsureDataDirectory();
        UpdateService.CleanupOldUpdateCache();

        try
        {
            // Chrome Runtime é necessário para as páginas internas de extensões do Chromium.
            CefSharpSettings.RuntimeStyle = CefRuntimeStyle.Chrome;
            CefSharpSettings.ShutdownOnExit = true;

            var settings = new CefSettings
            {
                RootCachePath = AppPaths.ChromiumRootDirectory,
                CachePath = AppPaths.ChromiumProfileDirectory
            };

            // Permite usar o modo de desenvolvedor da página chrome://extensions.
            settings.CefCommandLineArgs.Add("enable-unsafe-extension-debugging", "1");

            Directory.CreateDirectory(settings.CachePath);
            if (!Cef.Initialize(settings))
            {
                MessageBox.Show(
                    "O Rumo não conseguiu iniciar o Chromium. Verifique se todos os arquivos do pacote estão na mesma pasta e tente novamente.",
                    Branding.Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "O Rumo não conseguiu iniciar o Chromium. Verifique se todos os arquivos do pacote estão na mesma pasta.\n\n" + exception.Message,
                Branding.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new BrowserForm());
    }

    /// <summary>
    /// Impede duas instâncias de escreverem a mesma sessão e o mesmo perfil.
    /// Espera um pouco porque uma atualização reinicia o app logo em seguida.
    /// </summary>
    private static Mutex? TryAcquireSingleInstance()
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var mutex = new Mutex(initiallyOwned: false, InstanceMutexName);

            try
            {
                if (mutex.WaitOne(TimeSpan.FromMilliseconds(250))) return mutex;
            }
            catch (AbandonedMutexException)
            {
                // A instância anterior morreu sem liberar; este processo assume.
                return mutex;
            }

            mutex.Dispose();
        }

        return null;
    }
}
