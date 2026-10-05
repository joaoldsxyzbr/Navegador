using System.Runtime.CompilerServices;
using Android.Webkit;

namespace Navegador.Platforms.Android;

internal static class WebViewDownloadIntegration
{
    private static readonly ConditionalWeakTable<WebView, BrowserDownloadListener> Listeners = new();

    public static void Configure(WebView webView)
    {
        if (Listeners.TryGetValue(webView, out _))
            return;

        var listener = new BrowserDownloadListener(webView.Context);
        Listeners.Add(webView, listener);
        webView.SetDownloadListener(listener);
    }
}
