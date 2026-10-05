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
#endif

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        return builder.Build();
    }
}
