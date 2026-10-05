namespace Navegador;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
#if ANDROID
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping(
            "SupportMultipleWindows",
            (handler, view) =>
            {
                handler.PlatformView.Settings.SetSupportMultipleWindows(false);
            });

        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping(
            "Downloads",
            (handler, view) =>
            {
                Platforms.Android.WebViewDownloadIntegration.Configure(handler.PlatformView);
            });
#endif

#if WINDOWS
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping(
            "Downloads",
            (handler, view) =>
            {
                Platforms.Windows.WebViewDownloadIntegration.Configure(handler.PlatformView);
            });
#endif

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        return builder.Build();
    }
}
