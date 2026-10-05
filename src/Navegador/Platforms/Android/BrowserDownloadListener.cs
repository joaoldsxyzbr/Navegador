using Android.App;
using Android.Content;
using Android.OS;
using Android.Webkit;
using Android.Widget;

namespace Navegador.Platforms.Android;

internal sealed class BrowserDownloadListener(Context context) : Java.Lang.Object, IDownloadListener
{
    private readonly Context _context = context;

    public void OnDownloadStart(
        string? url,
        string? userAgent,
        string? contentDisposition,
        string? mimetype,
        long contentLength)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            var uri = global::Android.Net.Uri.Parse(url);
            var request = new DownloadManager.Request(uri);
            var fileName = URLUtil.GuessFileName(url, contentDisposition, mimetype);

            request.SetTitle(fileName);
            request.SetDescription("Download pelo Navegador");
            request.SetNotificationVisibility(DownloadVisibility.VisibleNotifyCompleted);

            if (!string.IsNullOrWhiteSpace(mimetype))
                request.SetMimeType(mimetype);

            if (!string.IsNullOrWhiteSpace(userAgent))
                request.AddRequestHeader("User-Agent", userAgent);

            var cookies = CookieManager.Instance.GetCookie(url);

            if (!string.IsNullOrWhiteSpace(cookies))
                request.AddRequestHeader("Cookie", cookies);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                request.SetDestinationInExternalPublicDir(
                    global::Android.OS.Environment.DirectoryDownloads,
                    fileName);
            }
            else
            {
                request.SetDestinationInExternalFilesDir(
                    _context,
                    global::Android.OS.Environment.DirectoryDownloads,
                    fileName);
            }

            var downloadManager = _context.GetSystemService(Context.DownloadService) as DownloadManager;

            if (downloadManager is null)
                throw new InvalidOperationException("DownloadManager indisponível.");

            downloadManager.Enqueue(request);
            Toast.MakeText(_context, "Download iniciado.", ToastLength.Short)?.Show();
        }
        catch (Exception)
        {
            Toast.MakeText(_context, "Não foi possível iniciar o download.", ToastLength.Short)?.Show();
        }
    }
}
