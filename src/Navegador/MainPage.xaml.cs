using Navegador.Core;
using Navegador.Services;

namespace Navegador;

public partial class MainPage : ContentPage
{
    private const int HistoryMenuLimit = 30;

    private readonly List<BrowserTab> _tabs = [];
    private readonly BrowserDataStore _dataStore = new();
    private BrowserTab? _activeTab;

    public MainPage()
    {
        InitializeComponent();
        CreateTab(AddressResolver.HomeUrl);
    }

    private WebView? ActiveBrowser => _activeTab?.Browser;

    private void OnNewTabClicked(object? sender, EventArgs e)
    {
        CreateTab(AddressResolver.HomeUrl);
    }

    private void OnGoClicked(object? sender, EventArgs e)
    {
        Navigate(AddressEntry.Text);
    }

    private void OnAddressCompleted(object? sender, EventArgs e)
    {
        Navigate(AddressEntry.Text);
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        if (ActiveBrowser?.CanGoBack == true)
            ActiveBrowser.GoBack();
    }

    private void OnForwardClicked(object? sender, EventArgs e)
    {
        if (ActiveBrowser?.CanGoForward == true)
            ActiveBrowser.GoForward();
    }

    private void OnReloadClicked(object? sender, EventArgs e)
    {
        ActiveBrowser?.Reload();
    }

    private void OnHomeClicked(object? sender, EventArgs e)
    {
        Navigate(AddressResolver.HomeUrl);
    }

    private async void OnFavoriteClicked(object? sender, EventArgs e)
    {
        var tab = _activeTab;

        if (tab is null || string.IsNullOrWhiteSpace(tab.Url))
            return;

        var isBookmarked = await _dataStore.ToggleBookmarkAsync(tab.Url, tab.Title);
        FavoriteButton.Text = isBookmarked ? "★" : "☆";
    }

    private async void OnFavoritesClicked(object? sender, EventArgs e)
    {
        var bookmarks = await _dataStore.GetBookmarksAsync();

        if (bookmarks.Count == 0)
        {
            await DisplayAlertAsync("Favoritos", "Nenhum favorito salvo ainda.", "OK");
            return;
        }

        var options = bookmarks
            .Select((item, index) => $"{index + 1}. {LimitText(item.Title, 42)}")
            .ToArray();

        var selected = await DisplayActionSheetAsync("Favoritos", "Cancelar", null, options);
        var index = ParseSelectionIndex(selected, bookmarks.Count);

        if (index >= 0)
            Navigate(bookmarks[index].Url);
    }

    private async void OnHistoryClicked(object? sender, EventArgs e)
    {
        var history = await _dataStore.GetHistoryAsync();

        if (history.Count == 0)
        {
            await DisplayAlertAsync("Histórico", "O histórico ainda está vazio.", "OK");
            return;
        }

        var visibleHistory = history.Take(HistoryMenuLimit).ToList();
        var options = visibleHistory
            .Select((item, index) => $"{index + 1}. {LimitText(item.Title, 42)}")
            .ToArray();

        var selected = await DisplayActionSheetAsync("Histórico recente", "Cancelar", null, options);
        var index = ParseSelectionIndex(selected, visibleHistory.Count);

        if (index >= 0)
            Navigate(visibleHistory[index].Url);
    }

    private async void OnPrivateClicked(object? sender, EventArgs e)
    {
        try
        {
            await Platforms.PrivateBrowsingIntegration.OpenAsync();
        }
        catch (PlatformNotSupportedException ex)
        {
            await DisplayAlertAsync("Modo privado", ex.Message, "OK");
        }
        catch (Exception)
        {
            await DisplayAlertAsync(
                "Modo privado",
                "Não foi possível abrir a navegação privada.",
                "OK");
        }
    }

    private async void OnBrowserNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (sender is not WebView browser)
            return;

        var tab = _tabs.FirstOrDefault(item => ReferenceEquals(item.Browser, browser));

        if (tab is null || string.IsNullOrWhiteSpace(e.Url))
            return;

        tab.Url = e.Url;
        tab.Title = GetDisplayTitle(e.Url);
        tab.SelectButton.Text = LimitText(tab.Title, 22);

        if (ReferenceEquals(tab, _activeTab))
        {
            AddressEntry.Text = tab.Url;
            UpdateNavigationButtons();
            await UpdateFavoriteButtonAsync();
        }

        if (e.Result == WebNavigationResult.Success)
            await _dataStore.AddHistoryAsync(tab.Url, tab.Title);
    }

    private void CreateTab(string url)
    {
        var browser = new WebView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            IsVisible = false
        };

        browser.Navigated += OnBrowserNavigated;

        var selectButton = new Button
        {
            Text = "Nova aba",
            FontSize = 12,
            Padding = new Thickness(10, 6),
            MaximumWidthRequest = 170
        };

        var closeButton = new Button
        {
            Text = "×",
            FontSize = 14,
            Padding = new Thickness(8, 6),
            WidthRequest = 40
        };

        SemanticProperties.SetDescription(closeButton, "Fechar aba");

        var tabView = new HorizontalStackLayout
        {
            Spacing = 2
        };

        tabView.Children.Add(selectButton);
        tabView.Children.Add(closeButton);

        var tab = new BrowserTab(browser, tabView, selectButton)
        {
            Url = AddressResolver.Resolve(url),
            Title = "Nova aba"
        };

        selectButton.Clicked += (_, _) => ActivateTab(tab);
        closeButton.Clicked += (_, _) => CloseTab(tab);

        _tabs.Add(tab);
        TabsContainer.Children.Add(tabView);
        BrowserHost.Children.Add(browser);

        ActivateTab(tab);
        browser.Source = tab.Url;
    }

    private void ActivateTab(BrowserTab tab)
    {
        if (!_tabs.Contains(tab))
            return;

        _activeTab = tab;

        foreach (var item in _tabs)
        {
            var isActive = ReferenceEquals(item, tab);
            item.Browser.IsVisible = isActive;
            item.SelectButton.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
            item.SelectButton.Opacity = isActive ? 1 : 0.65;
        }

        AddressEntry.Text = tab.Url;
        UpdateNavigationButtons();
        _ = UpdateFavoriteButtonAsync();
    }

    private void CloseTab(BrowserTab tab)
    {
        var index = _tabs.IndexOf(tab);

        if (index < 0)
            return;

        var wasActive = ReferenceEquals(tab, _activeTab);

        BrowserHost.Children.Remove(tab.Browser);
        TabsContainer.Children.Remove(tab.TabView);
        _tabs.RemoveAt(index);

        if (_tabs.Count == 0)
        {
            CreateTab(AddressResolver.HomeUrl);
            return;
        }

        if (wasActive)
        {
            var nextIndex = Math.Min(index, _tabs.Count - 1);
            ActivateTab(_tabs[nextIndex]);
        }
    }

    private void Navigate(string? input)
    {
        var browser = ActiveBrowser;

        if (browser is null)
            return;

        var url = AddressResolver.Resolve(input);
        AddressEntry.Text = url;
        browser.Source = url;
    }

    private void UpdateNavigationButtons()
    {
        BackButton.IsEnabled = ActiveBrowser?.CanGoBack == true;
        ForwardButton.IsEnabled = ActiveBrowser?.CanGoForward == true;
    }

    private async Task UpdateFavoriteButtonAsync()
    {
        var url = _activeTab?.Url;

        if (string.IsNullOrWhiteSpace(url))
        {
            FavoriteButton.Text = "☆";
            return;
        }

        FavoriteButton.Text = await _dataStore.IsBookmarkedAsync(url) ? "★" : "☆";
    }

    private static string GetDisplayTitle(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;

            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                host = host[4..];

            if (!string.IsNullOrWhiteSpace(host))
                return host;
        }

        return "Nova aba";
    }

    private static int ParseSelectionIndex(string? selected, int count)
    {
        if (string.IsNullOrWhiteSpace(selected) || selected == "Cancelar")
            return -1;

        var separator = selected.IndexOf('.');

        if (separator <= 0 ||
            !int.TryParse(selected[..separator], out var selectedNumber))
        {
            return -1;
        }

        var index = selectedNumber - 1;
        return index >= 0 && index < count ? index : -1;
    }

    private static string LimitText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Nova aba";

        return value.Length <= maxLength ? value : $"{value[..(maxLength - 1)]}…";
    }

    private sealed class BrowserTab(
        WebView browser,
        HorizontalStackLayout tabView,
        Button selectButton)
    {
        public WebView Browser { get; } = browser;
        public HorizontalStackLayout TabView { get; } = tabView;
        public Button SelectButton { get; } = selectButton;
        public string Url { get; set; } = AddressResolver.HomeUrl;
        public string Title { get; set; } = "Nova aba";
    }
}
