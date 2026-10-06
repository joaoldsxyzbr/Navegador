using CefSharp;
using CefSharp.WinForms;
using Navegador.Core;
using Navegador.Core.Models;
using Navegador.Core.Storage;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

internal sealed partial class BrowserForm : Form
{
    private const string AppName = Branding.Name;

    private static readonly Color TitleBarColor = Theme.TitleBar;
    private static readonly Color ToolbarColor = Theme.Toolbar;
    private static readonly Color ActiveTabColor = Theme.ActiveTab;
    private static readonly Color AddressColor = Theme.Address;
    private static readonly Color AddressFocusColor = Theme.AddressFocus;
    private static readonly Color HoverColor = Theme.Hover;
    private static readonly Color PressColor = Theme.Press;
    private static readonly Color TextColor = Theme.Text;
    private static readonly HttpClient FaviconClient = new() { Timeout = TimeSpan.FromSeconds(4) };

    private readonly FlowLayoutPanel _tabStrip = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = false,
        WrapContents = false,
        BackColor = TitleBarColor,
        Padding = new Padding(6, 0, 0, 0),
        Margin = Padding.Empty
    };

    private readonly FlowLayoutPanel _favoritesBar = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = false,
        WrapContents = false,
        BackColor = ToolbarColor,
        Padding = new Padding(6, 0, 6, 0),
        Margin = Padding.Empty
    };

    private readonly Panel _pageHost = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Theme.PageBackground,
        Margin = Padding.Empty
    };

    private readonly ListBox _addressSuggestions = new()
    {
        Visible = false,
        IntegralHeight = false,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 48,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = ActiveTabColor,
        ForeColor = TextColor,
        Font = Theme.Ui(9F),
        TabStop = false
    };

    private readonly TextBox _address = new()
    {
        Dock = DockStyle.Fill,
        BackColor = AddressColor,
        ForeColor = TextColor,
        BorderStyle = BorderStyle.None,
        Font = Theme.Ui(10.25F),
        PlaceholderText = "Pesquisar no Google ou digitar um endereço",
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
    private readonly List<string> _closedTabs = [];
    private readonly FavoritesStore _favorites;
    private readonly HistoryStore _history;
    private readonly SettingsStore _settings;
    private readonly DownloadManager _downloads;
    private readonly DownloadsBar _downloadsBar;
    private readonly IRequestContext? _requestContext;

    private readonly IconButton _newTabButton;
    private readonly IconButton _backButton;
    private readonly IconButton _forwardButton;
    private readonly IconButton _reloadButton;
    private readonly IconButton _bookmarkButton;
    private readonly IconButton _downloadsButton;
    private readonly IconButton _updateButton;
    private readonly IconButton _extensionsButton;
    private readonly IconButton _menuButton;
    private readonly WindowButton _maximizeButton;
    private readonly ContextMenuStrip _browserMenu;
    private readonly RowStyle _favoritesRowStyle;
    private readonly bool _isPrivate;
    private TableLayoutPanel? _rootLayout;
    private BrowserTab? _dragCandidate;
    private Point _dragStartPoint;
    private DateTimeOffset? _historyClearedAt;

    private BrowserTab? _activeTab;
    private bool _closingForGood;

    public BrowserForm(bool isPrivate = false)
    {
        _isPrivate = isPrivate;
        _settings = SettingsStore.Load();
        _favorites = FavoritesStore.Load();
        _history = HistoryStore.Load();
        _requestContext = _isPrivate
            ? new RequestContext(new RequestContextSettings
            {
                CachePath = string.Empty
            })
            : null;
        _downloads = new DownloadManager(_settings, this);
        _downloadsBar = new DownloadsBar(_downloads);

        Text = _isPrivate ? $"Navegação privada — {AppName}" : AppName;
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        MinimumSize = new Size(860, 560);
        Size = new Size(1360, 860);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = TitleBarColor;
        ForeColor = TextColor;
        FormBorderStyle = FormBorderStyle.None;
        KeyPreview = true;
        DoubleBuffered = true;

        _newTabButton = CreateIconButton(BrowserIcon.Add, "Nova guia", TitleBarColor);
        _newTabButton.Size = new Size(34, 34);
        _newTabButton.Margin = new Padding(4, 4, 0, 4);
        _newTabButton.Click += async (_, _) => await AddTabAsync();
        if (_isPrivate)
        {
            _tabStrip.Controls.Add(new Label
            {
                Text = "Privada",
                Width = 64,
                Height = 30,
                Margin = new Padding(8, 8, 0, 0),
                BackColor = Theme.Hover,
                ForeColor = Theme.Accent,
                Font = Theme.Ui(8.5F),
                TextAlign = ContentAlignment.MiddleCenter,
                AccessibleName = "Janela privada"
            });
        }
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

        _backButton = CreateIconButton(BrowserIcon.Back, "Voltar", ToolbarColor);
        _forwardButton = CreateIconButton(BrowserIcon.Forward, "Avançar", ToolbarColor);
        _reloadButton = CreateIconButton(BrowserIcon.Reload, "Recarregar", ToolbarColor);
        _bookmarkButton = CreateIconButton(BrowserIcon.Star, "Adicionar aos favoritos", ToolbarColor);
        _bookmarkButton.Enabled = !_isPrivate;
        _downloadsButton = CreateIconButton(BrowserIcon.Download, "Downloads", ToolbarColor);
        _updateButton = CreateIconButton(BrowserIcon.Update, $"Atualizar {AppName}", ToolbarColor);
        _extensionsButton = CreateIconButton(BrowserIcon.Extensions, "Extensões", ToolbarColor);
        _menuButton = CreateIconButton(BrowserIcon.Menu, "Menu", ToolbarColor);

        _backButton.Click += (_, _) => NavigateBack();
        _forwardButton.Click += (_, _) => NavigateForward();
        _reloadButton.Click += (_, _) => ReloadActiveTab();
        _bookmarkButton.Click += (_, _) => ToggleFavoriteForActiveTab();
        _downloadsButton.Click += (_, _) => ShowDownloads();
        _updateButton.Click += async (_, _) => await CheckForUpdatesAsync();
        _extensionsButton.Click += (_, _) => OpenExtensions();

        _browserMenu = BuildBrowserMenu();
        _menuButton.Click += (_, _) => _browserMenu.Show(_menuButton, new Point(0, _menuButton.Height));

        _addressShell.BorderColor = Theme.AddressBorder;
        _addressShell.BorderWidth = 1F;
        _addressShell.Controls.Add(_address);
        _address.Enter += (_, _) =>
        {
            _addressShell.BackColor = AddressFocusColor;
            _addressShell.BorderColor = Theme.Accent;
            _address.BackColor = AddressFocusColor;
        };
        _address.Leave += (_, _) =>
        {
            _addressShell.BackColor = AddressColor;
            _addressShell.BorderColor = Theme.AddressBorder;
            _address.BackColor = AddressColor;
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(() =>
                {
                    if (!_address.Focused && !_addressSuggestions.Focused)
                        _addressSuggestions.Visible = false;
                }));
            }
        };
        _address.TextChanged += (_, _) => RefreshAddressSuggestions();
        _addressSuggestions.DrawItem += DrawAddressSuggestion;
        _addressSuggestions.MouseDown += (_, eventArgs) =>
        {
            if (eventArgs.Button != MouseButtons.Left) return;
            var index = _addressSuggestions.IndexFromPoint(eventArgs.Location);
            if (index < 0 || index >= _addressSuggestions.Items.Count) return;
            _addressSuggestions.SelectedIndex = index;
            NavigateSelectedSuggestion();
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
            ColumnCount = 9,
            RowCount = 1,
            BackColor = ToolbarColor,
            Margin = Padding.Empty,
            Padding = new Padding(6, 2, 6, 2)
        };
        for (var index = 0; index < 9; index++)
        {
            toolbar.ColumnStyles.Add(new ColumnStyle(
                index == 3 ? SizeType.Percent : SizeType.Absolute,
                index == 3 ? 100 : 42));
        }

        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        toolbar.Controls.Add(_backButton, 0, 0);
        toolbar.Controls.Add(_forwardButton, 1, 0);
        toolbar.Controls.Add(_reloadButton, 2, 0);
        toolbar.Controls.Add(_addressShell, 3, 0);
        toolbar.Controls.Add(_bookmarkButton, 4, 0);
        toolbar.Controls.Add(_downloadsButton, 5, 0);
        toolbar.Controls.Add(_updateButton, 6, 0);
        toolbar.Controls.Add(_extensionsButton, 7, 0);
        toolbar.Controls.Add(_menuButton, 8, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = TitleBarColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        _favoritesRowStyle = new RowStyle(SizeType.Absolute, 0);
        root.RowStyles.Add(_favoritesRowStyle);
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));

        root.Controls.Add(titleRow, 0, 0);
        root.Controls.Add(toolbar, 0, 1);
        root.Controls.Add(_favoritesBar, 0, 2);
        root.Controls.Add(_pageHost, 0, 3);
        root.Controls.Add(_downloadsBar, 0, 4);

        var downloadsRowStyle = root.RowStyles[4];
        _downloadsBar.VisibleChanged += (_, _) =>
            downloadsRowStyle.Height = _downloadsBar.Visible ? _downloadsBar.Height : 0;
        downloadsRowStyle.Height = 0;
        _downloadsBar.Height = 120;
        _downloadsBar.Visible = false;

        _rootLayout = root;
        Controls.Add(root);
        Controls.Add(_addressSuggestions);
        _addressSuggestions.BringToFront();
        Deactivate += (_, _) => _addressSuggestions.Visible = false;

        Resize += (_, _) =>
        {
            _maximizeButton.Text = WindowState == FormWindowState.Maximized ? "❐" : "□";
            Padding = _isFullscreen || WindowState == FormWindowState.Maximized ? Padding.Empty : new Padding(1);
        };

        Shown += async (_, _) =>
        {
            await RestoreOrStartAsync();
        };

        FormClosing += OnFormClosing;
    }

    private IconButton CreateIconButton(BrowserIcon icon, string accessibleName, Color background)
    {
        var button = new IconButton
        {
            Icon = icon,
            AccessibleName = accessibleName,
            Dock = DockStyle.Fill,
            BackColor = background,
            ForeColor = TextColor,
            Font = Theme.Icon(),
            Margin = new Padding(3, 5, 3, 5)
        };

        _toolTip.SetToolTip(button, accessibleName);
        return button;
    }

    private WindowButton CreateWindowButton(string text, string accessibleName, bool closeButton = false)
    {
        var button = new WindowButton(closeButton)
        {
            Text = text,
            AccessibleName = accessibleName,
            Dock = DockStyle.Fill,
            Font = Theme.Ui(closeButton ? 13F : 10F)
        };

        _toolTip.SetToolTip(button, accessibleName);
        return button;
    }

    private BrowserTab? ActiveTab => _activeTab;

    private ChromiumWebBrowser? ActiveCore => _activeTab?.View;
}
