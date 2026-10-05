using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Navegador.Core;
using AndroidButton = Android.Widget.Button;
using AndroidEditText = Android.Widget.EditText;
using AndroidLinearLayout = Android.Widget.LinearLayout;
using AndroidOrientation = Android.Widget.Orientation;
using AndroidToast = Android.Widget.Toast;
using AndroidToastLength = Android.Widget.ToastLength;
using AndroidViewGroup = Android.Views.ViewGroup;
using AndroidWebView = Android.Webkit.WebView;
using AndroidWebViewClient = Android.Webkit.WebViewClient;
using CookieManager = Android.Webkit.CookieManager;
using WebStorage = Android.Webkit.WebStorage;

namespace Navegador.Platforms.Android;

[Activity(
    Label = "Navegador privado",
    Theme = "@style/Maui.MainTheme.NoActionBar",
    Exported = false,
    Process = ":private",
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
public sealed class PrivateBrowserActivity : Activity
{
    private static int _dataDirectoryConfigured;

    private AndroidWebView? _browser;
    private AndroidEditText? _address;
    private AndroidButton? _backButton;
    private AndroidButton? _forwardButton;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Build.VERSION.SdkInt < BuildVersionCodes.P)
        {
            AndroidToast.MakeText(
                this,
                "Modo privado requer Android 9 ou mais recente.",
                AndroidToastLength.Long)?.Show();
            Finish();
            return;
        }

        if (Interlocked.Exchange(ref _dataDirectoryConfigured, 1) == 0)
            AndroidWebView.SetDataDirectorySuffix("private");

        BuildInterface();
        _ = StartPrivateSessionAsync();
    }

    private void BuildInterface()
    {
        var root = new AndroidLinearLayout(this)
        {
            Orientation = AndroidOrientation.Vertical
        };

        root.SetPadding(ToPixels(8), ToPixels(8), ToPixels(8), ToPixels(8));

        var toolbar = new AndroidLinearLayout(this)
        {
            Orientation = AndroidOrientation.Horizontal
        };

        toolbar.SetGravity(GravityFlags.CenterVertical);

        _backButton = CreateButton("‹", "Voltar");
        _forwardButton = CreateButton("›", "Avançar");
        var reloadButton = CreateButton("↻", "Atualizar");
        var goButton = CreateButton("Ir", "Ir para o endereço");

        _address = new AndroidEditText(this)
        {
            Hint = "Digite um endereço ou pesquise"
        };

        _address.SetSingleLine(true);
        _address.LayoutParameters = new AndroidLinearLayout.LayoutParams(
            0,
            AndroidViewGroup.LayoutParams.WrapContent,
            1f);

        _backButton.Click += (_, _) =>
        {
            if (_browser?.CanGoBack() == true)
                _browser.GoBack();
        };

        _forwardButton.Click += (_, _) =>
        {
            if (_browser?.CanGoForward() == true)
                _browser.GoForward();
        };

        reloadButton.Click += (_, _) => _browser?.Reload();
        goButton.Click += (_, _) => Navigate(_address.Text);

        toolbar.AddView(_backButton);
        toolbar.AddView(_forwardButton);
        toolbar.AddView(reloadButton);
        toolbar.AddView(_address);
        toolbar.AddView(goButton);

        _browser = new AndroidWebView(this);

        var settings = _browser.Settings;
        settings.JavaScriptEnabled = true;
        settings.DomStorageEnabled = true;
        settings.SetSupportMultipleWindows(false);

        _browser.SetWebViewClient(new PrivateWebViewClient(url =>
        {
            if (_address is not null && !string.IsNullOrWhiteSpace(url))
                _address.Text = url;

            UpdateNavigationButtons();
        }));

        _browser.SetDownloadListener(new BrowserDownloadListener(this));

        root.AddView(
            toolbar,
            new AndroidLinearLayout.LayoutParams(
                AndroidViewGroup.LayoutParams.MatchParent,
                AndroidViewGroup.LayoutParams.WrapContent));

        root.AddView(
            _browser,
            new AndroidLinearLayout.LayoutParams(
                AndroidViewGroup.LayoutParams.MatchParent,
                0,
                1f));

        SetContentView(root);
        UpdateNavigationButtons();
    }

    private AndroidButton CreateButton(string text, string description)
    {
        var button = new AndroidButton(this)
        {
            Text = text,
            ContentDescription = description
        };

        button.SetMinWidth(ToPixels(44));
        return button;
    }

    private void Navigate(string? input)
    {
        if (_browser is null)
            return;

        var url = AddressResolver.Resolve(input);

        if (_address is not null)
            _address.Text = url;

        _browser.LoadUrl(url);
    }

    private void UpdateNavigationButtons()
    {
        if (_backButton is not null)
            _backButton.Enabled = _browser?.CanGoBack() == true;

        if (_forwardButton is not null)
            _forwardButton.Enabled = _browser?.CanGoForward() == true;
    }

    private async Task StartPrivateSessionAsync()
    {
        try
        {
            await ClearPrivateDataAsync(_browser);

            if (_browser is not null && !IsFinishing)
                Navigate(AddressResolver.HomeUrl);
        }
        catch
        {
            AndroidToast.MakeText(
                this,
                "Não foi possível limpar os dados privados. A navegação não foi iniciada.",
                AndroidToastLength.Long)?.Show();
            Finish();
        }
    }

    private async Task ClearPrivateDataAsync(AndroidWebView? browser)
    {
        browser?.StopLoading();
        browser?.ClearHistory();
        browser?.ClearCache(true);
        global::Android.Webkit.WebViewDatabase.GetInstance(this).ClearFormData();
        WebStorage.Instance.DeleteAllData();

        var cookies = CookieManager.Instance;
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        cookies.RemoveAllCookies(new CookieRemovalCallback(completion));
        await completion.Task;
        cookies.Flush();
    }

    private async Task ClearPrivateDataQuietlyAsync(AndroidWebView browser)
    {
        try
        {
            await ClearPrivateDataAsync(browser);
        }
        catch
        {
            // The next private session awaits a successful cleanup before it can navigate.
        }
    }

    private sealed class CookieRemovalCallback(TaskCompletionSource<bool> completion)
        : Java.Lang.Object, Android.Webkit.IValueCallback
    {
        public void OnReceiveValue(Java.Lang.Object? value) =>
            completion.TrySetResult(true);
    }

    public override void OnBackPressed()
    {
        if (_browser?.CanGoBack() == true)
        {
            _browser.GoBack();
            return;
        }

        base.OnBackPressed();
    }

    protected override void OnDestroy()
    {
        if (_browser is not null)
        {
            var browser = _browser;
            _browser = null;
            _ = ClearPrivateDataQuietlyAsync(browser);
            browser.LoadUrl("about:blank");
            browser.RemoveAllViews();
            browser.Destroy();
        }

        base.OnDestroy();
    }

    private int ToPixels(int dp)
    {
        return (int)Math.Round(dp * Resources!.DisplayMetrics!.Density);
    }

    private sealed class PrivateWebViewClient(Action<string?> onPageFinished) : AndroidWebViewClient
    {
        public override void OnPageFinished(AndroidWebView? view, string? url)
        {
            base.OnPageFinished(view, url);
            onPageFinished(url);
        }
    }
}
