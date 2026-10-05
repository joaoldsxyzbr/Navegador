using Microsoft.Maui.Storage;

namespace Navegador.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        var userDataFolder = Path.Combine(FileSystem.Current.AppDataDirectory, "WebView2");
        Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);

        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
