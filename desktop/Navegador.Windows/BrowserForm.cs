using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Navegador.Windows;

internal sealed class BrowserForm : Form
{
    private const string HomeUrl = "https://www.google.com/";
    private const int ResizeBorder = 6;
    private const int WmNcHitTest = 0x0084;
    private const int WmNcLButtonDown = 0x00A1;
    private const int HtClient = 1;
    private const int HtCaption = 2;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;

    private static readonly Color TitleBarColor = Color.FromArgb(32, 33, 36);
    private static readonly Color ToolbarColor = Color.FromArgb(41, 42, 45);
    private static readonly Color ActiveTabColor = Color.FromArgb(53, 54, 58);
    private static readonly Color AddressColor = Color.FromArgb(48, 49, 52);
    private static readonly Color AddressFocusColor = Color.FromArgb(57, 58, 62);
    private static readonly Color HoverColor = Color.FromArgb(60, 64, 67);
    private static readonly Color PressColor = Color.FromArgb(74, 76, 80);
    private static readonly Color TextColor = Color.FromArgb(232, 234, 237);
    private static readonly Color MutedTextColor = Color.FromArgb(154, 160, 166);
    private static readonly Color CloseHoverColor = Color.FromArgb(196, 43, 28);

    private readonly FlowLayoutPanel _tabStrip = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = false,
        WrapContents = false,
        BackColor = TitleBarColor,
        Padding = new Padding(6, 0, 0, 0),
        Margin = Padding.Empty
    };

    private readonly Panel _pageHost = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.White,
        Margin = Padding.Empty
    };

    private readonly TextBox _address = new()
    {
        Dock = DockStyle.Fill,
        BackColor = AddressColor,
        ForeColor = TextColor,
        BorderStyle = BorderStyle.None,
        Font = new Font("Segoe UI", 10.25F),
        PlaceholderText = "Pesquisar no Google ou digitar um URL",
        AutoSize = false,
        Margin = Padding.Empty
    };

    private readonly RoundedPanel _addressShell = new(18)
    {
        Dock = DockStyle.Fill,
        BackColor = AddressColor,
        Padding = new Padding(14, 8, 14, 6),
        Margin = new Padding(8, 4, 8, 4)
    };

    private readonly ToolTip _toolTip = new()
    {
        AutomaticDelay = 450,
        AutoPopDelay = 5000,
        ReshowDelay = 100
    };

    private readonly List<BrowserTab> _tabs = [];
    private readonly ChromeIconButton _newTabButton;
    private readonly ChromeIconButton _backButton;
    private readonly ChromeIconButton _forwardButton;
    private readonly ChromeIconButton _reloadButton;
    private readonly ChromeIconButton _updateButton;
    private readonly ChromeIconButton _extensionsButton;
    private readonly ChromeIconButton _menuButton;
    private readonly Button _maximizeButton;
    private readonly ContextMenuStrip _browserMenu;

    private Task<CoreWebView2Environment>? _environmentTask;
    private BrowserTab? _activeTab;
    private int _nextTabNumber = 1;

    public BrowserForm()
    {
        Text = "Navegador";
        MinimumSize = new Size(820, 560);
        Size = new Size(1360, 860);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = TitleBarColor;
        ForeColor = TextColor;
        FormBorderStyle = FormBorderStyle.None;
        KeyPreview = true;
        DoubleBuffered = true;

        _newTabButton = CreateIconButton("+", "Nova guia", TitleBarColor, new Font("Segoe UI", 14F));
        _newTabButton.Size = new Size(34, 34);
        _newTabButton.Margin = new Padding(4, 4, 0, 4);
        _newTabButton.Click += async (_, _) => await AddTabAsync();
        _tabStrip.Controls.Add(_newTabButton);
        _tabStrip.MouseDown += BeginWindowDrag;
        _tabStrip.DoubleClick += (_, _) => ToggleMaximize();

        var minimizeButton = CreateWindowButton("—", "Minimizar");
        _maximizeButton = CreateWindowButton("□", "Maximizar");
        var closeButton = CreateWindowButton("×", "Fechar", closeButton: true);

        minimizeButton.Click += (_, _) => WindowState = FormWindowState.Minimized;
        _maximizeButton.Click += (_, _) => ToggleMaximize();
        closeButton.Click += (_, _) => Close();

        var titleRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = TitleBarColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        titleRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        titleRow.Controls.Add(_tabStrip, 0, 0);
        titleRow.Controls.Add(minimizeButton, 1, 0);
        titleRow.Controls.Add(_maximizeButton, 2, 0);
        titleRow.Controls.Add(closeButton, 3, 0);

        _backButton = CreateIconButton("←", "Voltar", ToolbarColor);
        _forwardButton = CreateIconButton("→", "Avançar", ToolbarColor);
        _reloadButton = CreateIconButton("↻", "Recarregar", ToolbarColor, new Font("Segoe UI Symbol", 13F));
        _updateButton = CreateIconButton("⇩", "Atualizar Navegador", ToolbarColor, new Font("Segoe UI Symbol", 12F));
        _extensionsButton = CreateIconButton("🧩", "Extensões", ToolbarColor, new Font("Segoe UI Emoji", 10.5F));
        _menuButton = CreateIconButton("⋮", "Menu", ToolbarColor, new Font("Segoe UI", 15F));

        _backButton.Click += (_, _) => NavigateBack();
        _forwardButton.Click += (_, _) => NavigateForward();
        _reloadButton.Click += (_, _) => _activeTab?.View.CoreWebView2?.Reload();
        _updateButton.Click += async (_, _) => await CheckForUpdatesAsync();
        _extensionsButton.Click += (_, _) => OpenExtensions();

        _browserMenu = BuildBrowserMenu();
        _menuButton.Click += (_, _) => _browserMenu.Show(_menuButton, new Point(0, _menuButton.Height));

        _addressShell.Controls.Add(_address);
        _address.Enter += (_, _) =>
        {
            _addressShell.BackColor = AddressFocusColor;
            _address.BackColor = AddressFocusColor;
        };
        _address.Leave += (_, _) =>
        {
            _addressShell.BackColor = AddressColor;
            _address.BackColor = AddressColor;
        };
        _address.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode != Keys.Enter) return;
            eventArgs.SuppressKeyPress = true;
            NavigateAddress();
        };

        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1,
            BackColor = ToolbarColor,
            Margin = Padding.Empty,
            Padding = new Padding(6, 2, 6, 2)
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        toolbar.Controls.Add(_backButton, 0, 0);
        toolbar.Controls.Add(_forwardButton, 1, 0);
        toolbar.Controls.Add(_reloadButton, 2, 0);
        toolbar.Controls.Add(_addressShell, 3, 0);
        toolbar.Controls.Add(_updateButton, 4, 0);
        toolbar.Controls.Add(_extensionsButton, 5, 0);
        toolbar.Controls.Add(_menuButton, 6, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = TitleBarColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(titleRow, 0, 0);
        root.Controls.Add(toolbar, 0, 1);
        root.Controls.Add(_pageHost, 0, 2);
        Controls.Add(root);

        LocationChanged += (_, _) => UpdateMaximizedBounds();
        Resize += (_, _) =>
        {
            _maximizeButton.Text = WindowState == FormWindowState.Maximized ? "❐" : "□";
            Padding = WindowState == FormWindowState.Maximized ? Padding.Empty : new Padding(1);
        };
        Shown += async (_, _) =>
        {
            UpdateMaximizedBounds();
            await AddTabAsync();
        };
    }

    private ChromeIconButton CreateIconButton(string text, string accessibleName, Color background, Font? font = null)
    {
        var button = new ChromeIconButton
        {
            Text = text,
            AccessibleName = accessibleName,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = background,
            ForeColor = TextColor,
            Font = font ?? new Font("Segoe UI", 11.5F),
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            Margin = new Padding(3, 4, 3, 4),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = HoverColor;
        button.FlatAppearance.MouseDownBackColor = PressColor;
        _toolTip.SetToolTip(button, accessibleName);
        return button;
    }

    private Button CreateWindowButton(string text, string accessibleName, bool closeButton = false)
    {
        var button = new Button
        {
            Text = text,
            AccessibleName = accessibleName,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = TitleBarColor,
            ForeColor = TextColor,
            Font = new Font("Segoe UI", closeButton ? 13F : 10F),
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            Margin = Padding.Empty
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = closeButton ? CloseHoverColor : HoverColor;
        button.FlatAppearance.MouseDownBackColor = closeButton ? CloseHoverColor : PressColor;
        _toolTip.SetToolTip(button, accessibleName);
        return button;
    }

    private ContextMenuStrip BuildBrowserMenu()
    {
        var menu = new ContextMenuStrip
        {
            BackColor = ActiveTabColor,
            ForeColor = TextColor,
            ShowImageMargin = false,
            Font = new Font("Segoe UI", 10F),
            Padding = new Padding(4)
        };

        var newTabItem = new ToolStripMenuItem("Nova guia");
        newTabItem.Click += async (_, _) => await AddTabAsync();

        var updateItem = new ToolStripMenuItem("Atualizar Navegador");
        updateItem.Click += async (_, _) => await CheckForUpdatesAsync();

        var extensionsItem = new ToolStripMenuItem("Extensões");
        extensionsItem.Click += (_, _) => OpenExtensions();

        var closeTabItem = new ToolStripMenuItem("Fechar guia");
        closeTabItem.Click += (_, _) =>
        {
            if (_activeTab is not null) CloseTab(_activeTab);
        };

        foreach (var item in new[] { newTabItem, updateItem, extensionsItem, closeTabItem })
        {
            item.BackColor = ActiveTabColor;
            item.ForeColor = TextColor;
        }

        menu.Items.Add(newTabItem);
        menu.Items.Add(updateItem);
        menu.Items.Add(extensionsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(closeTabItem);
        return menu;
    }

    private async Task AddTabAsync(string? initialAddress = null)
    {
        var view = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.FromArgb(32, 33, 36)
        };

        var tab = new BrowserTab(_nextTabNumber++, view);
        _toolTip.SetToolTip(tab.CloseButton, "Fechar guia");
        tab.SelectButton.Click += (_, _) => ActivateTab(tab);
        tab.CloseButton.Click += (_, _) => CloseTab(tab);

        _tabs.Add(tab);
        _tabStrip.Controls.Add(tab.Header);
        _tabStrip.Controls.SetChildIndex(_newTabButton, _tabStrip.Controls.Count - 1);
        _pageHost.Controls.Add(view);
        ActivateTab(tab);

        try
        {
            var environment = await GetEnvironmentAsync();
            await view.EnsureCoreWebView2Async(environment);
            AttachBrowserEvents(tab);
            view.CoreWebView2.Navigate(initialAddress ?? HomeUrl);
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

    private async Task CheckForUpdatesAsync()
    {
        if (!_updateButton.Enabled) return;

        var originalText = _updateButton.Text;
        _updateButton.Enabled = false;
        _updateButton.Text = "…";

        try
        {
            await UpdateService.CheckAndInstallAsync(this);
        }
        finally
        {
            if (!IsDisposed)
            {
                _updateButton.Text = originalText;
                _updateButton.Enabled = true;
            }
        }
    }

    private void OpenExtensions()
    {
        var profile = _activeTab?.View.CoreWebView2?.Profile;
        if (profile is null)
        {
            MessageBox.Show(
                this,
                "Aguarde a guia terminar de iniciar.",
                "Extensões",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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

        core.NewWindowRequested += (_, eventArgs) =>
        {
            eventArgs.Handled = true;
            _ = AddTabAsync(eventArgs.Uri);
        };
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
        _address.Text = tab.View.Source?.ToString() ?? string.Empty;
        UpdateNavigationButtons(tab);
    }

    private void CloseTab(BrowserTab tab)
    {
        var index = _tabs.IndexOf(tab);
        if (index < 0) return;

        _tabs.Remove(tab);
        _tabStrip.Controls.Remove(tab.Header);
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
            ? "Nova guia"
            : Shorten(title, 26);
    }

    private void UpdateNavigationButtons(BrowserTab tab)
    {
        var core = tab.View.CoreWebView2;
        SetNavigationState(_backButton, core?.CanGoBack ?? false);
        SetNavigationState(_forwardButton, core?.CanGoForward ?? false);
        SetNavigationState(_reloadButton, core is not null);
    }

    private static void SetNavigationState(Button button, bool enabled)
    {
        button.Enabled = enabled;
        button.ForeColor = enabled ? TextColor : MutedTextColor;
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

    private void ToggleMaximize()
    {
        if (WindowState == FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Normal;
            return;
        }

        UpdateMaximizedBounds();
        WindowState = FormWindowState.Maximized;
    }

    private void UpdateMaximizedBounds()
    {
        if (!IsHandleCreated) return;
        MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
    }

    private void BeginWindowDrag(object? sender, MouseEventArgs eventArgs)
    {
        if (eventArgs.Button != MouseButtons.Left) return;

        if (WindowState == FormWindowState.Maximized)
            WindowState = FormWindowState.Normal;

        ReleaseCapture();
        SendMessage(Handle, WmNcLButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
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

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmNcHitTest && WindowState == FormWindowState.Normal)
        {
            base.WndProc(ref m);
            if ((int)m.Result != HtClient) return;

            var packed = m.LParam.ToInt64();
            var screenPoint = new Point(
                unchecked((short)(packed & 0xFFFF)),
                unchecked((short)((packed >> 16) & 0xFFFF)));
            var clientPoint = PointToClient(screenPoint);

            var left = clientPoint.X <= ResizeBorder;
            var right = clientPoint.X >= ClientSize.Width - ResizeBorder;
            var top = clientPoint.Y <= ResizeBorder;
            var bottom = clientPoint.Y >= ClientSize.Height - ResizeBorder;

            if (left && top) m.Result = (IntPtr)HtTopLeft;
            else if (right && top) m.Result = (IntPtr)HtTopRight;
            else if (left && bottom) m.Result = (IntPtr)HtBottomLeft;
            else if (right && bottom) m.Result = (IntPtr)HtBottomRight;
            else if (left) m.Result = (IntPtr)HtLeft;
            else if (right) m.Result = (IntPtr)HtRight;
            else if (top) m.Result = (IntPtr)HtTop;
            else if (bottom) m.Result = (IntPtr)HtBottom;
            return;
        }

        base.WndProc(ref m);
    }

    private static string Shorten(string value, int length) =>
        value.Length <= length ? value : value[..(length - 1)] + "…";

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private sealed class BrowserTab
    {
        public BrowserTab(int number, WebView2 view)
        {
            Number = number;
            View = view;

            Header = new RoundedPanel(11)
            {
                Width = 220,
                Height = 36,
                Margin = new Padding(3, 6, 0, 0),
                BackColor = TitleBarColor
            };

            SelectButton = new Button
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = TitleBarColor,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9.5F),
                Text = "Nova guia",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };
            SelectButton.FlatAppearance.BorderSize = 0;
            SelectButton.FlatAppearance.MouseOverBackColor = HoverColor;

            CloseButton = new Button
            {
                Dock = DockStyle.Right,
                Width = 34,
                Text = "×",
                AccessibleName = "Fechar guia",
                FlatStyle = FlatStyle.Flat,
                BackColor = TitleBarColor,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 11F),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };
            CloseButton.FlatAppearance.BorderSize = 0;
            CloseButton.FlatAppearance.MouseOverBackColor = HoverColor;
            CloseButton.FlatAppearance.MouseDownBackColor = PressColor;

            Header.Controls.Add(SelectButton);
            Header.Controls.Add(CloseButton);
        }

        public int Number { get; }
        public WebView2 View { get; }
        public RoundedPanel Header { get; }
        public Button SelectButton { get; }
        public Button CloseButton { get; }
    }

    private sealed class RoundedPanel : Panel
    {
        private readonly int _radius;

        public RoundedPanel(int radius)
        {
            _radius = radius;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnResize(EventArgs eventArgs)
        {
            base.OnResize(eventArgs);
            UpdateRoundedRegion();
        }

        private void UpdateRoundedRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using var path = CreateRoundedPath(new Rectangle(0, 0, Width, Height), _radius);
            Region?.Dispose();
            Region = new Region(path);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 1)
            {
                path.AddRectangle(bounds);
                return path;
            }

            var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private sealed class ChromeIconButton : Button
    {
        protected override void OnResize(EventArgs eventArgs)
        {
            base.OnResize(eventArgs);
            if (Width <= 0 || Height <= 0) return;

            var diameter = Math.Min(Width, Height);
            using var path = new GraphicsPath();
            path.AddEllipse((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
            Region?.Dispose();
            Region = new Region(path);
        }
    }
}
