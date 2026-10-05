using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Navegador.Core;
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
    private EditText? _address;
    private Button? _backButton;
    private Button? _forwardButton;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Build.VERSION.SdkInt < BuildVersionCodes.P)
        {
            Toast.MakeText(
                this,
                "Modo privado requer Android 9 ou mais recente.",
                ToastLength.Long)?.Show();
            Finish();
            return;
        }

        if (Interlocked.Exchange(ref _dataDirectoryConfigured, 1) == 0)
            AndroidWebView.SetDataDirectorySuffix("private");

        BuildInterface();
        ClearPrivateData();
        Navigate(AddressResolver.HomeUrl);
    }

    private void BuildInterface()
    {
        var root = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        root.SetPadding(ToPixels(8), ToPixels(8), ToPixels(8), ToPixels(8));

        var toolbar = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal,
            Gravity = GravityFlags.CenterVertical
        };

        _backButton = CreateButton("‹", "Voltar");
        _forwardButton = CreateButton("›", "Avançar");
        var reloadButton = CreateButton("↻", "Atualizar");
        var goButton = CreateButton("Ir", "Ir para o endereço");

        _address = new EditText(this)
        {
            Hint = "Digite um endereço ou pesquise",
            SingleLine = true
        };

        _address.LayoutParameters = new LinearLayout.LayoutParams(
            0,
            ViewGroup.LayoutParams.WrapContent,
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
            new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent));

        root.AddView(
            _browser,
            new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                0,
                1f));

        SetContentView(root);
        UpdateNavigationButtons();
    }

    private Button CreateButton(string text, string description)
    {
        var button = new Button(this)
        {
            Text = text,
            ContentDescription = description,
            MinWidth = ToPixels(44)
        };

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

    private void ClearPrivateData()
    {
        _browser?.StopLoading();
        _browser?.ClearHistory();
        _browser?.ClearCache(true);
        _browser?.ClearFormData();

        CookieManager.Instance.RemoveAllCookies(null);
        CookieManager.Instance.Flush();
        WebStorage.Instance.DeleteAllData();
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
            ClearPrivateData();
            _browser.LoadUrl("about:blank");
            _browser.RemoveAllViews();
            _browser.Destroy();
            _browser = null;
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
