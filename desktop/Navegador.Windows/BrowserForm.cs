using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Navegador.Windows;

internal sealed class BrowserForm : Form
{
    private static readonly Color WindowColor = Color.FromArgb(32, 33, 36);
    private static readonly Color SurfaceColor = Color.FromArgb(48, 49, 52);
    private static readonly Color ActiveTabColor = Color.FromArgb(60, 64, 67);
    private static readonly Color TextColor = Color.FromArgb(232, 234, 237);

    private readonly FlowLayoutPanel _tabHeaders = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = false,
        BackColor = WindowColor,
        Padding = new Padding(4, 4, 0, 0)
    };
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill, BackColor = Color.White };
    private readonly TextBox _address = new()
    {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        BackColor = SurfaceColor,
        ForeColor = TextColor,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10F),
        PlaceholderText = "Pesquisar ou digitar endereço"
    };
    private readonly Button _backButton;
    private readonly Button _forwardButton;
    private readonly Button _reloadButton;
    private readonly Button _goButton;
    private readonly Button _extensionsButton;
    private readonly List<BrowserTab> _tabs = [];
    private Task<CoreWebView2Environment>? _environmentTask;
    private BrowserTab? _activeTab;
    private int _nextTabNumber = 1;

    public BrowserForm()
    {
        Text = "Navegador";
        MinimumSize = new Size(720, 480);
        Size = new Size(1280, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = WindowColor;
        ForeColor = TextColor;
        KeyPreview = true;

        var tabsBar = new Panel { Dock = DockStyle.Fill, BackColor = WindowColor };
        var newTabButton = CreateButton("+", "Nova aba");
        newTabButton.Dock = DockStyle.Right;
        newTabButton.Width = 44;
        newTabButton.Click += async (_, _) => await AddTabAsync();
        tabsBar.Controls.Add(_tabHeaders);
        tabsBar.Controls.Add(newTabButton);

        var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = WindowColor };
        _backButton = CreateButton("‹", "Voltar");
        _forwardButton = CreateButton("›", "Avançar");
        _reloadButton = CreateButton("⟳", "Recarregar");
        var homeButton = CreateButton("⌂", "Nova guia");
        _goButton = CreateButton("Ir", "Abrir endereço");
        _extensionsButton = CreateButton("Ext", "Extensões");
        _goButton.Width = 48;
        _extensionsButton.Width = 48;

        _backButton.SetBounds(6, 5, 36, 36);
        _forwardButton.SetBounds(44, 5, 36, 36);
        _reloadButton.SetBounds(82, 5, 36, 36);
        homeButton.SetBounds(120, 5, 36, 36);
        _address.SetBounds(162, 8, 900, 30);
        _goButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _extensionsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _goButton.SetBounds(0, 5, 48, 36);
        _extensionsButton.SetBounds(0, 5, 48, 36);

        _backButton.Click += (_, _) => NavigateBack();
        _forwardButton.Click += (_, _) => NavigateForward();
        _reloadButton.Click += (_, _) => _activeTab?.View.CoreWebView2?.Reload();
        homeButton.Click += (_, _) => NavigateToHome();
        _goButton.Click += (_, _) => NavigateAddress();
        _extensionsButton.Click += (_, _) => OpenExtensions();
        _address.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode != Keys.Enter) return;
            eventArgs.SuppressKeyPress = true;
            NavigateAddress();
        };
        toolbar.Controls.AddRange([_backButton, _forwardButton, _reloadButton, homeButton, _address, _extensionsButton, _goButton]);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = WindowColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(tabsBar, 0, 0);
        layout.Controls.Add(toolbar, 0, 1);
        layout.Controls.Add(_pageHost, 0, 2);
        Controls.Add(layout);

        Resize += (_, _) => LayoutAddressBar();
        Shown += async (_, _) => await AddTabAsync();
    }

    private void LayoutAddressBar()
    {
        var width = Math.Max(160, ClientSize.Width - 330);
        _address.Width = width;
        _extensionsButton.Left = ClientSize.Width - 112;
        _goButton.Left = ClientSize.Width - 58;
    }

    private static Button CreateButton(string text, string accessibleName)
    {
        var button = new Button
        {
            Text = text,
            AccessibleName = accessibleName,
            FlatStyle = FlatStyle.Flat,
            BackColor = WindowColor,
            ForeColor = TextColor,
            Font = new Font("Segoe UI", 10F),
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ActiveTabColor;
        return button;
    }

    private async Task AddTabAsync(string? initialAddress = null)
    {
        var view = new WebView2 { Dock = DockStyle.Fill };
        var tab = new BrowserTab(_nextTabNumber++, view);
        tab.SelectButton.Click += (_, _) => ActivateTab(tab);
        tab.CloseButton.Click += (_, _) => CloseTab(tab);
        _tabs.Add(tab);
        _tabHeaders.Controls.Add(tab.Header);
        _pageHost.Controls.Add(view);
        ActivateTab(tab);

        try
        {
            var environment = await GetEnvironmentAsync();
            await view.EnsureCoreWebView2Async(environment);
            AttachBrowserEvents(tab);
            view.CoreWebView2.Navigate(initialAddress ?? "about:blank");
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                "Não foi possível iniciar o WebView2. Verifique se o Microsoft Edge WebView2 Runtime está instalado.\n\n" +
                exception.Message + "\n\nhttps://developer.microsoft.com/microsoft-edge/webview2/",
                "Navegador",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        _environmentTask ??= CreateEnvironmentAsync();
        return _environmentTask;
    }

    private static Task<CoreWebView2Environment> CreateEnvironmentAsync()
    {
        var options = new CoreWebView2EnvironmentOptions
        {
            AreBrowserExtensionsEnabled = true
        };
        return CoreWebView2Environment.CreateAsync(
            userDataFolder: GetProfileDirectory(),
            options: options);
    }

    private static string GetProfileDirectory()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "WebView2");
        Directory.CreateDirectory(path);
        return path;
    }

    private void OpenExtensions()
    {
        var profile = _activeTab?.View.CoreWebView2?.Profile;
        if (profile is null)
        {
            MessageBox.Show(this, "Aguarde a aba terminar de iniciar.", "Extensões",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new ExtensionsForm(profile);
        dialog.ShowDialog(this);
    }

    private void AttachBrowserEvents(BrowserTab tab)
    {
        var core = tab.View.CoreWebView2;
        core.NavigationStarting += (_, eventArgs) =>
        {
            if (_activeTab == tab) _address.Text = eventArgs.Uri;
            UpdateNavigationButtons(tab);
        };
        core.SourceChanged += (_, _) =>
        {
            if (_activeTab == tab) _address.Text = core.Source;
        };
        core.DocumentTitleChanged += (_, _) => UpdateTabTitle(tab);
        core.NavigationCompleted += (_, _) =>
        {
            UpdateTabTitle(tab);
            UpdateNavigationButtons(tab);
            if (_activeTab == tab) _address.Text = core.Source;
        };
    }

    private void ActivateTab(BrowserTab tab)
    {
        _activeTab = tab;
        foreach (var item in _tabs)
        {
            item.Header.BackColor = item == tab ? ActiveTabColor : SurfaceColor;
            item.SelectButton.BackColor = item == tab ? ActiveTabColor : SurfaceColor;
            item.View.Visible = item == tab;
        }

        tab.View.BringToFront();
        _address.Text = tab.View.Source?.ToString() ?? string.Empty;
        UpdateNavigationButtons(tab);
        UpdateWindowTitle(tab);
    }

    private void CloseTab(BrowserTab tab)
    {
        var index = _tabs.IndexOf(tab);
        if (index < 0) return;

        _tabs.Remove(tab);
        _tabHeaders.Controls.Remove(tab.Header);
        _pageHost.Controls.Remove(tab.View);
        tab.View.Dispose();
        tab.Header.Dispose();

        if (_tabs.Count == 0)
        {
            _ = AddTabAsync();
            return;
        }

        if (_activeTab == tab)
            ActivateTab(_tabs[Math.Min(index, _tabs.Count - 1)]);
    }

    private void UpdateTabTitle(BrowserTab tab)
    {
        var title = tab.View.CoreWebView2?.DocumentTitle;
        tab.SelectButton.Text = string.IsNullOrWhiteSpace(title)
            ? $"Nova guia {tab.Number}"
            : Shorten(title, 20);
        if (_activeTab == tab) UpdateWindowTitle(tab);
    }

    private void UpdateWindowTitle(BrowserTab tab)
    {
        var title = tab.View.CoreWebView2?.DocumentTitle;
        Text = string.IsNullOrWhiteSpace(title) ? "Navegador" : $"{title} — Navegador";
    }

    private void UpdateNavigationButtons(BrowserTab tab)
    {
        var core = tab.View.CoreWebView2;
        _backButton.Enabled = core?.CanGoBack ?? false;
        _forwardButton.Enabled = core?.CanGoForward ?? false;
        _reloadButton.Enabled = core is not null;
    }

    private void NavigateAddress()
    {
        var input = _address.Text.Trim();
        if (input.Length == 0 || _activeTab?.View.CoreWebView2 is not { } core) return;
        core.Navigate(ResolveAddress(input));
    }

    private static string ResolveAddress(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return uri.ToString();
        }

        if (!input.Any(char.IsWhiteSpace) &&
            (input.Contains('.') || input.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)))
        {
            return "https://" + input;
        }

        return "https://www.google.com/search?q=" + Uri.EscapeDataString(input);
    }

    private void NavigateBack()
    {
        var core = _activeTab?.View.CoreWebView2;
        if (core?.CanGoBack == true) core.GoBack();
    }

    private void NavigateForward()
    {
        var core = _activeTab?.View.CoreWebView2;
        if (core?.CanGoForward == true) core.GoForward();
    }

    private void NavigateToHome()
    {
        if (_activeTab?.View.CoreWebView2 is { } core)
        {
            core.Navigate("about:blank");
            _address.Focus();
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.L))
        {
            _address.Focus();
            _address.SelectAll();
            return true;
        }
        if (keyData == (Keys.Control | Keys.T))
        {
            _ = AddTabAsync();
            return true;
        }
        if (keyData == (Keys.Control | Keys.W))
        {
            if (_activeTab is not null) CloseTab(_activeTab);
            return true;
        }
        if (keyData == (Keys.Control | Keys.R))
        {
            _activeTab?.View.CoreWebView2?.Reload();
            return true;
        }
        if (keyData == (Keys.Alt | Keys.Left))
        {
            NavigateBack();
            return true;
        }
        if (keyData == (Keys.Alt | Keys.Right))
        {
            NavigateForward();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static string Shorten(string value, int length) =>
        value.Length <= length ? value : value[..(length - 1)] + "…";

    private sealed class BrowserTab
    {
        public BrowserTab(int number, WebView2 view)
        {
            Number = number;
            View = view;
            Header = new Panel
            {
                Width = 190,
                Height = 32,
                Margin = new Padding(2, 0, 2, 0),
                BackColor = SurfaceColor
            };
            SelectButton = new Button
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = SurfaceColor,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9F),
                Text = $"Nova guia {number}",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                UseVisualStyleBackColor = false
            };
            SelectButton.FlatAppearance.BorderSize = 0;
            CloseButton = CreateButton("×", "Fechar aba");
            CloseButton.Dock = DockStyle.Right;
            CloseButton.Width = 32;
            Header.Controls.Add(SelectButton);
            Header.Controls.Add(CloseButton);
        }

        public int Number { get; }
        public WebView2 View { get; }
        public Panel Header { get; }
        public Button SelectButton { get; }
        public Button CloseButton { get; }
    }
}
