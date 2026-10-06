using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Navegador.Core;
using Navegador.Core.Models;
using Navegador.Core.Storage;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

internal sealed partial class BrowserForm
{
    /// <summary>Abre a sessão anterior quando o usuário pediu, senão a página inicial.</summary>
    private async Task RestoreOrStartAsync()
    {
        if (_settings.Current.RestoreSession)
        {
            var snapshot = SessionStore.Load();

            if (snapshot.Tabs.Count > 0)
            {
                var active = Math.Clamp(snapshot.ActiveIndex, 0, snapshot.Tabs.Count - 1);

                foreach (var tab in snapshot.Tabs)
                {
                    await AddTabAsync(tab.Url, activate: false);
                }

                if (_tabs.Count > 0) ActivateTab(_tabs[Math.Min(active, _tabs.Count - 1)]);
                RefreshFavoritesBar();
                return;
            }
        }

        await AddTabAsync();
        RefreshFavoritesBar();
    }

    private async Task<BrowserTab?> AddTabAsync(string? initialAddress = null, bool activate = true)
    {
        var view = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Theme.PageBackground,
            Visible = false
        };

        var tab = new BrowserTab(view);
        _toolTip.SetToolTip(tab.CloseButton, "Fechar guia");
        tab.SelectButton.Click += (_, _) => ActivateTab(tab);
        tab.CloseButton.Click += (_, _) => CloseTab(tab);

        _tabs.Add(tab);
        _tabStrip.Controls.Add(tab.Header);
        _tabStrip.Controls.SetChildIndex(_newTabButton, _tabStrip.Controls.Count - 1);
        _pageHost.Controls.Add(view);

        if (activate) ActivateTab(tab);

        try
        {
            var environment = await GetEnvironmentAsync();

            // A aba pode ter sido fechada enquanto o WebView2 inicializava.
            if (tab.View.IsDisposed || !_tabs.Contains(tab)) return null;

            await view.EnsureCoreWebView2Async(environment);

            if (tab.View.IsDisposed || !_tabs.Contains(tab)) return null;

            AttachBrowserEvents(tab);

            var target = AddressResolver.IsPersistable(initialAddress)
                ? initialAddress!
                : _settings.Current.HomeUrl;

            view.CoreWebView2!.Navigate(target);
            return tab;
        }
        catch (Exception exception)
        {
            if (tab.View.IsDisposed || !_tabs.Contains(tab)) return null;

            MessageBox.Show(
                this,
                "Não foi possível iniciar o WebView2. Verifique se o Microsoft Edge WebView2 Runtime está instalado.\n\n" +
                exception.Message + "\n\nhttps://developer.microsoft.com/microsoft-edge/webview2/",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return tab;
        }
    }

    /// <summary>Uma aba pode estar fechando: antes de agir, confirme que ela vive.</summary>
    private bool IsAlive(BrowserTab tab) => !tab.View.IsDisposed && _tabs.Contains(tab);

    private Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        _environmentTask ??= CreateEnvironmentAsync();
        return _environmentTask;
    }

    private static Task<CoreWebView2Environment> CreateEnvironmentAsync()
    {
        AppPaths.EnsureDataDirectory();

        var options = new CoreWebView2EnvironmentOptions
        {
            AreBrowserExtensionsEnabled = true
        };

        return CoreWebView2Environment.CreateAsync(
            userDataFolder: AppPaths.WebViewProfileDirectory,
            options: options);
    }

    private void ActivateTab(BrowserTab tab)
    {
        _activeTab = tab;

        foreach (var item in _tabs)
        {
            var active = item == tab;
            var background = active ? ActiveTabColor : TitleBarColor;

            item.Header.BackColor = background;
            item.SelectButton.BackColor = background;
            item.SelectButton.FlatAppearance.MouseOverBackColor = active ? ActiveTabColor : HoverColor;
            item.CloseButton.BackColor = background;
            item.CloseButton.Visible = active;
            item.View.Visible = active;
        }

        tab.View.BringToFront();

        var source = tab.View.Source?.ToString();
        _address.Text = source ?? string.Empty;

        UpdateNavigationButtons(tab);
        RefreshBookmarkButton();
        UpdateWindowTitle();
    }

    private void CloseTab(BrowserTab tab)
    {
        var index = _tabs.IndexOf(tab);
        if (index < 0) return;

        var wasActive = _activeTab == tab;

        _tabs.Remove(tab);
        _tabStrip.Controls.Remove(tab.Header);
        _pageHost.Controls.Remove(tab.View);

        if (wasActive) _activeTab = null;

        tab.View.Dispose();
        tab.Header.Dispose();

        if (_tabs.Count == 0)
        {
            _ = AddTabAsync();
            return;
        }

        if (wasActive) ActivateTab(_tabs[Math.Min(index, _tabs.Count - 1)]);
    }

    private void CloseActiveTab()
    {
        if (_activeTab is not null) CloseTab(_activeTab);
    }

    private void UpdateTabTitle(BrowserTab tab)
    {
        var title = tab.View.CoreWebView2?.DocumentTitle;
        tab.SelectButton.Text = string.IsNullOrWhiteSpace(title)
            ? "Nova guia"
            : Shorten(title, 26);

        if (tab == _activeTab) UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        var title = _activeTab?.View.CoreWebView2?.DocumentTitle;

        Text = string.IsNullOrWhiteSpace(title) || title == "Nova guia"
            ? AppName
            : $"{Shorten(title, 60)} — {AppName}";
    }

    private void UpdateNavigationButtons(BrowserTab tab)
    {
        var core = tab.View.CoreWebView2;

        SetNavigationState(_backButton, core?.CanGoBack ?? false);
        SetNavigationState(_forwardButton, core?.CanGoForward ?? false);
        SetNavigationState(_reloadButton, core is not null);
    }

    private static void SetNavigationState(IconButton button, bool enabled)
    {
        button.Enabled = enabled;
    }

    /// <summary>Todas as abas vivas, na ordem visual, para salvar a sessão.</summary>
    private SessionSnapshot CaptureSession()
    {
        var snapshot = new SessionSnapshot { Tabs = [], ActiveIndex = 0 };

        foreach (var tab in _tabs)
        {
            var url = tab.View.Source?.ToString();
            if (!AddressResolver.IsPersistable(url)) continue;

            if (tab == _activeTab) snapshot.ActiveIndex = snapshot.Tabs.Count;

            snapshot.Tabs.Add(new SessionTab
            {
                Url = url!,
                Title = tab.View.CoreWebView2?.DocumentTitle ?? url!
            });
        }

        return snapshot;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (_closingForGood) return;
        _closingForGood = true;

        if (!_settings.Current.RestoreSession)
        {
            // Sem restauração o usuário não espera encontrar as abas de volta.
            SessionStore.Clear();
        }
        else
        {
            SessionStore.Save(CaptureSession());
        }

        _history.Save();
        _favorites.Save();
        _settings.Save();
        _downloads.Dispose();

        foreach (var tab in _tabs.ToList())
        {
            _tabStrip.Controls.Remove(tab.Header);
            _pageHost.Controls.Remove(tab.View);
            tab.View.Dispose();
            tab.Header.Dispose();
        }

        _tabs.Clear();
    }

    private static string Shorten(string value, int length)
    {
        var single = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return single.Length <= length ? single : single[..(length - 1)] + "…";
    }

    private sealed class BrowserTab
    {
        public BrowserTab(WebView2 view)
        {
            View = view;

            Header = new RoundedPanel(11)
            {
                Width = 220,
                Height = 36,
                Margin = new Padding(3, 6, 0, 0),
                BackColor = Theme.TitleBar
            };

            SelectButton = new Button
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.TitleBar,
                ForeColor = Theme.Text,
                Font = Theme.Ui(9.5F),
                Text = "Nova guia",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            SelectButton.FlatAppearance.BorderSize = 0;
            SelectButton.FlatAppearance.MouseOverBackColor = Theme.Hover;

            CloseButton = new Button
            {
                Dock = DockStyle.Right,
                Width = 34,
                Text = "×",
                AccessibleName = "Fechar guia",
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.TitleBar,
                ForeColor = Theme.Text,
                Font = Theme.Icon(11F),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            CloseButton.FlatAppearance.BorderSize = 0;
            CloseButton.FlatAppearance.MouseOverBackColor = Theme.Hover;
            CloseButton.FlatAppearance.MouseDownBackColor = Theme.Press;

            Header.Controls.Add(SelectButton);
            Header.Controls.Add(CloseButton);
        }

        public WebView2 View { get; }

        public RoundedPanel Header { get; }

        public Button SelectButton { get; }

        public Button CloseButton { get; }
    }
}
