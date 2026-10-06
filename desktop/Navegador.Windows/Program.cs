using Navegador.Core;
using Navegador.Core.Updates;

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
                "O Navegador já está aberto.",
                "Navegador",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        AppPaths.EnsureDataDirectory();
        UpdateService.CleanupOldUpdateCache();

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
