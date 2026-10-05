using Microsoft.Maui.Storage;
using Microsoft.Web.WebView2.Core;
using Navegador.Core;
using WinUIControls = Microsoft.UI.Xaml.Controls;
using WinUIGridLength = Microsoft.UI.Xaml.GridLength;
using WinUIGridUnitType = Microsoft.UI.Xaml.GridUnitType;
using WinUIThickness = Microsoft.UI.Xaml.Thickness;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace Navegador.Platforms;

internal static class PrivateBrowsingIntegration
{
    private static readonly List<PrivateBrowserWindow> OpenWindows = [];

    public static async Task OpenAsync()
    {
        var window = new PrivateBrowserWindow();

        window.Closed += (_, _) => OpenWindows.Remove(window);
        OpenWindows.Add(window);
        window.Activate();

        try
        {
            await window.InitializeAsync();
        }
        catch
        {
            OpenWindows.Remove(window);
            window.Close();
            throw;
        }
    }

    private sealed class PrivateBrowserWindow : WinUIWindow
    {
        private readonly WinUIControls.WebView2 _browser = new();
        private readonly WinUIControls.TextBox _address = new()
        {
            PlaceholderText = "Digite um endereço ou pesquise"
        };

        private readonly WinUIControls.Button _backButton = new()
        {
            Content = "‹",
            MinWidth = 44
        };

        private readonly WinUIControls.Button _forwardButton = new()
        {
            Content = "›",
            MinWidth = 44
        };

        public PrivateBrowserWindow()
        {
            Title = "Navegador • Privado";
            Content = BuildInterface();
        }

        public async Task InitializeAsync()
        {
            var userDataFolder = Path.Combine(
                FileSystem.Current.AppDataDirectory,
                "WebView2");

            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            var options = environment.CreateCoreWebView2ControllerOptions();
            options.ProfileName = "NavegadorPrivate";
            options.IsInPrivateModeEnabled = true;

            await _browser.EnsureCoreWebView2Async(environment, options);

            if (_browser.CoreWebView2 is null)
                throw new InvalidOperationException("WebView2 privado não foi inicializado.");

            Navegador.Platforms.Windows.WebViewDownloadIntegration.Configure(_browser);
            _browser.CoreWebView2.NavigationCompleted += (_, _) => UpdateNavigationState();
            Navigate(AddressResolver.HomeUrl);
        }

        private WinUIControls.Grid BuildInterface()
        {
            var root = new WinUIControls.Grid
            {
                RowSpacing = 8,
                Padding = new WinUIThickness(8)
            };

            root.RowDefinitions.Add(new WinUIControls.RowDefinition
            {
                Height = WinUIGridLength.Auto
            });

            root.RowDefinitions.Add(new WinUIControls.RowDefinition
            {
                Height = new WinUIGridLength(1, WinUIGridUnitType.Star)
            });

            var toolbar = new WinUIControls.Grid
            {
                ColumnSpacing = 6
            };

            toolbar.ColumnDefinitions.Add(new WinUIControls.ColumnDefinition { Width = WinUIGridLength.Auto });
            toolbar.ColumnDefinitions.Add(new WinUIControls.ColumnDefinition { Width = WinUIGridLength.Auto });
            toolbar.ColumnDefinitions.Add(new WinUIControls.ColumnDefinition { Width = WinUIGridLength.Auto });
            toolbar.ColumnDefinitions.Add(new WinUIControls.ColumnDefinition
            {
                Width = new WinUIGridLength(1, WinUIGridUnitType.Star)
            });
            toolbar.ColumnDefinitions.Add(new WinUIControls.ColumnDefinition { Width = WinUIGridLength.Auto });

            var reloadButton = new WinUIControls.Button { Content = "↻", MinWidth = 44 };
            var goButton = new WinUIControls.Button { Content = "Ir", MinWidth = 52 };

            _backButton.Click += (_, _) =>
            {
                if (_browser.CoreWebView2?.CanGoBack == true)
                    _browser.CoreWebView2.GoBack();
            };

            _forwardButton.Click += (_, _) =>
            {
                if (_browser.CoreWebView2?.CanGoForward == true)
                    _browser.CoreWebView2.GoForward();
            };

            reloadButton.Click += (_, _) => _browser.CoreWebView2?.Reload();
            goButton.Click += (_, _) => Navigate(_address.Text);

            WinUIControls.Grid.SetColumn(_backButton, 0);
            WinUIControls.Grid.SetColumn(_forwardButton, 1);
            WinUIControls.Grid.SetColumn(reloadButton, 2);
            WinUIControls.Grid.SetColumn(_address, 3);
            WinUIControls.Grid.SetColumn(goButton, 4);

            toolbar.Children.Add(_backButton);
            toolbar.Children.Add(_forwardButton);
            toolbar.Children.Add(reloadButton);
            toolbar.Children.Add(_address);
            toolbar.Children.Add(goButton);

            WinUIControls.Grid.SetRow(toolbar, 0);
            WinUIControls.Grid.SetRow(_browser, 1);
            root.Children.Add(toolbar);
            root.Children.Add(_browser);

            UpdateNavigationState();
            return root;
        }

        private void Navigate(string? input)
        {
            var core = _browser.CoreWebView2;

            if (core is null)
                return;

            var url = AddressResolver.Resolve(input);
            _address.Text = url;
            core.Navigate(url);
        }

        private void UpdateNavigationState()
        {
            var core = _browser.CoreWebView2;
            _backButton.IsEnabled = core?.CanGoBack == true;
            _forwardButton.IsEnabled = core?.CanGoForward == true;

            if (core is not null && !string.IsNullOrWhiteSpace(core.Source))
                _address.Text = core.Source;
        }
    }
}
