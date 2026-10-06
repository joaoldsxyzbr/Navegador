using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;
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

        // Evita carregar o assembly C++/CLI do CefSharp antes de conferir
        // o runtime Visual C++ exigido pelo Chromium.
        if (!EnsureVisualCppRuntime()) return;

        AppPaths.EnsureDataDirectory();
        UpdateService.CleanupOldUpdateCache();

        if (!InitializeChromium()) return;

        Application.Run(new BrowserForm());
    }

    private static bool EnsureVisualCppRuntime()
    {
        try
        {
            if (IsVisualCppRuntimeInstalled()) return true;

            var installer = Path.Combine(AppContext.BaseDirectory, "VC_redist.x64.exe");
            if (!File.Exists(installer))
            {
                MessageBox.Show(
                    "O runtime Microsoft Visual C++ 2022 x64 não está instalado e o instalador VC_redist.x64.exe não foi encontrado ao lado do Rumo.",
                    Branding.Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            var answer = MessageBox.Show(
                "Para iniciar o Chromium, o Rumo precisa do runtime Microsoft Visual C++ 2022 x64. O instalador já está incluído. Deseja instalá-lo agora? O Windows pedirá autorização de administrador.",
                Branding.Name,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (answer != DialogResult.Yes) return false;

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = installer,
                Arguments = "/install /passive /norestart",
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true,
                Verb = "runas"
            });

            if (process is null)
            {
                ShowVisualCppInstallError("O instalador não pôde ser iniciado.");
                return false;
            }

            process.WaitForExit();
            if (process.ExitCode is not (0 or 3010) || !IsVisualCppRuntimeInstalled())
            {
                ShowVisualCppInstallError($"O instalador terminou com o código {process.ExitCode} e o runtime não foi confirmado.");
                return false;
            }

            return true;
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            ShowVisualCppInstallError("A instalação foi cancelada. Autorize a instalação do runtime para abrir o navegador.");
            return false;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "Não foi possível verificar ou instalar o runtime necessário para o Chromium.\n\n" + exception.Message,
                Branding.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    private static bool IsVisualCppRuntimeInstalled()
    {
        using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var runtimeKey = localMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");
        if (runtimeKey is null) return false;

        var installed = Convert.ToInt32(runtimeKey.GetValue("Installed", 0));
        var major = Convert.ToInt32(runtimeKey.GetValue("Major", 0));
        var minor = Convert.ToInt32(runtimeKey.GetValue("Minor", 0));
        return VisualCppRuntimePolicy.IsSupported(installed, major, minor);
    }

    private static void ShowVisualCppInstallError(string details)
    {
        MessageBox.Show(
            $"O Rumo não conseguiu preparar o runtime Microsoft Visual C++ 2022 x64.\n\n{details}\n\nVocê também pode executar VC_redist.x64.exe como administrador na pasta Rumo.",
            Branding.Name,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static bool InitializeChromium()
    {
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
            if (Cef.Initialize(settings)) return true;

            MessageBox.Show(
                "O Rumo não conseguiu iniciar o Chromium. Verifique se todos os arquivos do pacote estão na mesma pasta e tente novamente.",
                Branding.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "O Rumo não conseguiu iniciar o Chromium. Verifique se todos os arquivos do pacote estão na mesma pasta.\n\n" + exception.Message,
                Branding.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
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
