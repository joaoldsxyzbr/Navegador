namespace Navegador.Windows;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (UpdateService.TryRunApplyUpdate(args))
            return;

        UpdateService.CleanupOldUpdateCache();
        Application.Run(new BrowserForm());
    }
}
