using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.Core.Content;

namespace Navegador.Platforms.Android;

internal static class UpdateInstaller
{
    public static Task<bool> InstallAsync(string packagePath)
    {
        if (!File.Exists(packagePath) ||
            !string.Equals(Path.GetExtension(packagePath), ".apk", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Não foi possível localizar o pacote Android.");
        }

        var context = global::Android.App.Application.Context;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O &&
            context.PackageManager?.CanRequestPackageInstalls() != true)
        {
            var settingsIntent = new Intent(
                global::Android.Provider.Settings.ActionManageUnknownAppSources,
                global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
            settingsIntent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(settingsIntent);
            Toast.MakeText(
                context,
                "Permita a instalação para o Navegador e toque em Atualizar agora novamente.",
                ToastLength.Long)?.Show();
            return Task.FromResult(false);
        }

        var file = new Java.IO.File(packagePath);
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
            context,
            $"{context.PackageName}.fileprovider",
            file);
        var installIntent = new Intent(Intent.ActionView);
        installIntent.SetDataAndType(uri, "application/vnd.android.package-archive");
        installIntent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
        context.StartActivity(installIntent);
        return Task.FromResult(true);
    }
}
