using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Navegador.Platforms.Windows;

internal static class WebViewDownloadIntegration
{
    private static readonly ConditionalWeakTable<CoreWebView2, object> ConfiguredWebViews = new();

    public static void Configure(WebView2 webView)
    {
        webView.CoreWebView2Initialized -= OnCoreWebView2Initialized;
        webView.CoreWebView2Initialized += OnCoreWebView2Initialized;

        if (webView.CoreWebView2 is not null)
            ConfigureCore(webView.CoreWebView2);
    }

    private static void OnCoreWebView2Initialized(
        WebView2 sender,
        CoreWebView2InitializedEventArgs args)
    {
        if (args.Exception is null && sender.CoreWebView2 is not null)
            ConfigureCore(sender.CoreWebView2);
    }

    private static void ConfigureCore(CoreWebView2 coreWebView)
    {
        if (ConfiguredWebViews.TryGetValue(coreWebView, out _))
            return;

        ConfiguredWebViews.Add(coreWebView, new object());
        coreWebView.DownloadStarting += OnDownloadStarting;
    }

    private static void OnDownloadStarting(
        object? sender,
        CoreWebView2DownloadStartingEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ResultFilePath))
            return;

        args.ResultFilePath = GetUniquePath(args.ResultFilePath);
        args.Handled = false;
    }

    private static string GetUniquePath(string path)
    {
        if (!File.Exists(path))
            return path;

        var directory = Path.GetDirectoryName(path);

        if (string.IsNullOrWhiteSpace(directory))
            return path;

        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var suffix = 1;
        string candidate;

        do
        {
            candidate = Path.Combine(directory, $"{name} ({suffix}){extension}");
            suffix++;
        }
        while (File.Exists(candidate));

        return candidate;
    }
}
