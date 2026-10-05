using Android.Content;
using Android.OS;

namespace Navegador.Platforms;

internal static class PrivateBrowsingIntegration
{
    public static Task OpenAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.P)
        {
            throw new PlatformNotSupportedException(
                "O modo privado com isolamento real requer Android 9 ou mais recente.");
        }

        var context = global::Android.App.Application.Context;
        var intent = new Intent(
            context,
            typeof(Navegador.Platforms.Android.PrivateBrowserActivity));

        intent.AddFlags(ActivityFlags.NewTask);
        context.StartActivity(intent);

        return Task.CompletedTask;
    }
}
