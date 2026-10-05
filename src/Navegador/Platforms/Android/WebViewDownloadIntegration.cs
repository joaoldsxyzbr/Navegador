using System.Runtime.CompilerServices;
using AndroidWebView = Android.Webkit.WebView;

namespace Navegador.Platforms.Android;

internal static class WebViewDownloadIntegration
{
    private static readonly ConditionalWeakTable<AndroidWebView, BrowserDownloadListener> Listeners = new();

    public static void Configure(AndroidWebView webView)
    {
        if (Listeners.TryGetValue(webView, out _))
            return;

        var listener = new BrowserDownloadListener(webView.Context);
        Listeners.Add(webView, listener);
        webView.SetDownloadListener(listener);
    }
}
