using System.Diagnostics;
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
        if (_isPrivate)
        {
            await AddTabAsync(_settings.Current.HomeUrl);
            RefreshFavoritesBar();
            return;
        }

        if (_settings.Current.RestoreSession)
        {
            var snapshot = SessionStore.Load();

            if (snapshot.Tabs.Count > 0)
            {
                var active = Math.Clamp(snapshot.ActiveIndex, 0, snapshot.Tabs.Count - 1);

                foreach (var tab in snapshot.Tabs)
                {
                    await AddTabAsync(tab.Url, activate: false, pinned: tab.IsPinned);
                }

                if (_tabs.Count > 0) ActivateTab(_tabs[Math.Min(active, _tabs.Count - 1)]);
                RefreshFavoritesBar();
                return;
            }
        }

        await AddTabAsync(_settings.Current.HomeUrl);
        RefreshFavoritesBar();
    }

    private async Task<BrowserTab?> AddTabAsync(string? initialAddress = null, bool activate = true, bool pinned = false)
    {
        var view = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Theme.PageBackground,
            Visible = false
        };

        var tab = new BrowserTab(view, pinned);
        _toolTip.SetToolTip(tab.CloseButton, "Fechar guia");
        tab.SelectButton.Click += (_, _) => ActivateTab(tab);
        tab.CloseButton.Click += (_, _) => CloseTab(tab);
        AttachTabDragHandlers(tab);
        ConfigureTabContextMenu(tab);

        _tabs.Add(tab);
        _tabStrip.Controls.Add(tab.Header);
        _tabStrip.Controls.SetChildIndex(_newTabButton, _tabStrip.Controls.Count - 1);
        _pageHost.Controls.Add(view);
        if (tab.IsPinned) MoveTab(tab, _tabs.Count(item => item.IsPinned) - 1);

        if (activate) ActivateTab(tab);

        try
        {
            var environment = await GetEnvironmentAsync();

            // A aba pode ter sido fechada enquanto o WebView2 inicializava.
            if (tab.View.IsDisposed || !_tabs.Contains(tab)) return null;

            var controllerOptions = _isPrivate ? environment.CreateCoreWebView2ControllerOptions() : null;
            if (controllerOptions is not null) controllerOptions.IsInPrivateModeEnabled = true;
            await view.EnsureCoreWebView2Async(environment, controllerOptions);

            if (tab.View.IsDisposed || !_tabs.Contains(tab)) return null;

            AttachBrowserEvents(tab);

            if (string.IsNullOrWhiteSpace(initialAddress))
            {
                ShowNewTabPage(tab);
            }
            else
            {
                var target = AddressResolver.IsPersistable(initialAddress) ? initialAddress! : AddressResolver.Resolve(initialAddress);
                if (string.IsNullOrWhiteSpace(target)) ShowNewTabPage(tab);
                else view.CoreWebView2!.Navigate(target);
            }

            return tab;
        }
        catch (Exception exception)
        {
            if (tab.View.IsDisposed || !_tabs.Contains(tab)) return null;

            if (exception is WebView2RuntimeNotFoundException)
            {
                var install = MessageBox.Show(
                    this,
                    "O Rumo precisa do Microsoft Edge WebView2 Runtime para abrir páginas.\n\n" +
                    "Quer abrir a página oficial para baixar e instalar o Runtime? Depois, feche e abra o Rumo novamente.",
                    AppName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (install == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo("https://developer.microsoft.com/microsoft-edge/webview2/")
                    {
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                MessageBox.Show(
                    this,
                    "Não foi possível iniciar o WebView2.\n\n" + exception.Message,
                    AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

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
            item.CloseButton.Visible = active && !item.IsPinned;
            item.View.Visible = active;
        }

        tab.View.BringToFront();

        var source = tab.View.Source?.ToString();
        _address.Text = tab.IsInternalNewTab ? string.Empty : source ?? string.Empty;

        UpdateNavigationButtons(tab);
        RefreshBookmarkButton();
        UpdateWindowTitle();
    }

    private void CloseTab(BrowserTab tab)
    {
        var index = _tabs.IndexOf(tab);
        if (index < 0) return;

        var url = tab.View.Source?.ToString();
        if (AddressResolver.IsPersistable(url)) RememberClosedTab(url!);

        var wasActive = _activeTab == tab;

        _tabs.Remove(tab);
        _tabStrip.Controls.Remove(tab.Header);
        _pageHost.Controls.Remove(tab.View);

        if (wasActive) _activeTab = null;

        tab.View.Dispose();
        tab.SelectButton.Image = null;
        tab.Favicon?.Dispose();
        tab.ContextMenu.Dispose();
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

    private void RememberClosedTab(string url)
    {
        _closedTabs.Add(url);
        if (_closedTabs.Count > 20) _closedTabs.RemoveAt(0);
    }

    private void ReopenClosedTab()
    {
        if (_closedTabs.Count == 0) return;
        var index = _closedTabs.Count - 1;
        var url = _closedTabs[index];
        _closedTabs.RemoveAt(index);
        _ = AddTabAsync(url);
    }

    private void ConfigureTabContextMenu(BrowserTab tab)
    {
        var menu = new ContextMenuStrip
        {
            BackColor = ActiveTabColor,
            ForeColor = TextColor,
            ShowImageMargin = false,
            Font = Theme.Ui(9F)
        };
        var pinItem = new ToolStripMenuItem();
        pinItem.Click += (_, _) => TogglePinnedTab(tab);
        var closeItem = new ToolStripMenuItem("Fechar guia");
        closeItem.Click += (_, _) => CloseTab(tab);
        menu.Items.AddRange([pinItem, new ToolStripSeparator(), closeItem]);
        menu.Opening += (_, _) => pinItem.Text = tab.IsPinned ? "Desafixar guia" : "Fixar guia";

        tab.ContextMenu = menu;
        tab.Header.ContextMenuStrip = menu;
        tab.SelectButton.ContextMenuStrip = menu;
    }

    private void TogglePinnedTab(BrowserTab tab)
    {
        if (!IsAlive(tab)) return;

        tab.IsPinned = !tab.IsPinned;
        tab.Header.Width = tab.IsPinned ? 58 : 238;
        var pinnedCount = _tabs.Count(item => item.IsPinned);
        MoveTab(tab, tab.IsPinned ? pinnedCount - 1 : pinnedCount);
        UpdateTabTitle(tab);
        if (tab == _activeTab) ActivateTab(tab);
    }

    private void AttachTabDragHandlers(BrowserTab tab)
    {
        foreach (var control in new Control[] { tab.Header, tab.SelectButton })
        {
            control.MouseDown += (_, eventArgs) => BeginTabDrag(tab, eventArgs);
            control.MouseMove += (_, eventArgs) => ContinueTabDrag(tab, eventArgs);
            control.MouseUp += (_, _) => EndTabDrag(tab);
        }
    }

    private void BeginTabDrag(BrowserTab tab, MouseEventArgs eventArgs)
    {
        if (eventArgs.Button != MouseButtons.Left) return;
        _dragCandidate = tab;
        _dragStartPoint = Cursor.Position;
    }

    private void ContinueTabDrag(BrowserTab tab, MouseEventArgs eventArgs)
    {
        if (_dragCandidate != tab || eventArgs.Button != MouseButtons.Left) return;

        var current = Cursor.Position;
        var threshold = SystemInformation.DragSize;
        if (Math.Abs(current.X - _dragStartPoint.X) < threshold.Width / 2 &&
            Math.Abs(current.Y - _dragStartPoint.Y) < threshold.Height / 2) return;

        var localX = _tabStrip.PointToClient(current).X;
        var targetIndex = _tabs.FindIndex(item => localX < item.Header.Left + item.Header.Width / 2);
        if (targetIndex < 0) targetIndex = _tabs.Count - 1;

        var pinnedCount = _tabs.Count(item => item.IsPinned);
        targetIndex = tab.IsPinned
            ? Math.Min(targetIndex, Math.Max(0, pinnedCount - 1))
            : Math.Max(targetIndex, pinnedCount);
        MoveTab(tab, targetIndex);
    }

    private void EndTabDrag(BrowserTab tab)
    {
        if (_dragCandidate == tab) _dragCandidate = null;
    }

    private void MoveTab(BrowserTab tab, int targetIndex)
    {
        var currentIndex = _tabs.IndexOf(tab);
        if (currentIndex < 0 || _tabs.Count == 0) return;

        targetIndex = Math.Clamp(targetIndex, 0, _tabs.Count - 1);
        if (currentIndex == targetIndex) return;

        _tabs.RemoveAt(currentIndex);
        _tabs.Insert(targetIndex, tab);
        _tabStrip.Controls.SetChildIndex(tab.Header, targetIndex);
        _tabStrip.Controls.SetChildIndex(_newTabButton, _tabStrip.Controls.Count - 1);
        _tabStrip.PerformLayout();
    }

    private async Task UpdateTabFaviconAsync(BrowserTab tab)
    {
        if (!IsAlive(tab) || tab.View.CoreWebView2 is not { } core) return;
        if (string.IsNullOrWhiteSpace(core.FaviconUri))
        {
            SetTabFavicon(tab, null);
            return;
        }

        try
        {
            using var stream = await core.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
            using var image = Image.FromStream(stream);
            var favicon = new Bitmap(image, new Size(18, 18));
            if (!IsAlive(tab))
            {
                favicon.Dispose();
                return;
            }

            SetTabFavicon(tab, favicon);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or IOException or ArgumentException or System.Runtime.InteropServices.COMException)
        {
            // Algumas páginas retiram o favicon antes de a imagem ficar disponível.
        }
    }

    private static void SetTabFavicon(BrowserTab tab, Image? favicon)
    {
        if (ReferenceEquals(tab.Favicon, favicon)) return;
        var previous = tab.Favicon;
        tab.Favicon = favicon;
        tab.SelectButton.Image = favicon;
        previous?.Dispose();
    }

    private void ShowNewTabPage(BrowserTab tab)
    {
        if (tab.View.CoreWebView2 is null) return;
        tab.IsInternalNewTab = true;
        SetTabFavicon(tab, null);
        tab.SelectButton.Text = tab.IsPinned ? string.Empty : "Nova guia";
        _toolTip.SetToolTip(tab.SelectButton, "Nova guia");
        tab.View.CoreWebView2.NavigateToString(NewTabPage.Build(_favorites.Items, _isPrivate));
        if (tab == _activeTab) { _address.Clear(); UpdateWindowTitle(); }
    }

    private void UpdateTabTitle(BrowserTab tab)
    {
        var title = tab.IsInternalNewTab ? "Nova guia" : tab.View.CoreWebView2?.DocumentTitle;
        var displayTitle = string.IsNullOrWhiteSpace(title) ? "Nova guia" : title;
        tab.SelectButton.Text = tab.IsPinned ? string.Empty : Shorten(displayTitle, 28);
        _toolTip.SetToolTip(tab.SelectButton, displayTitle);

        if (tab == _activeTab) UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        var title = _activeTab?.View.CoreWebView2?.DocumentTitle;

        var pageTitle = string.IsNullOrWhiteSpace(title) || title == "Nova guia"
            ? AppName
            : $"{Shorten(title, 60)} — {AppName}";
        Text = _isPrivate ? $"Navegação privada — {pageTitle}" : pageTitle;
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
                Title = tab.View.CoreWebView2?.DocumentTitle ?? url!,
                IsPinned = tab.IsPinned
            });
        }

        return snapshot;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (_closingForGood) return;
        _closingForGood = true;

        if (!_isPrivate)
        {
            if (!_settings.Current.RestoreSession)
            {
                // Sem restauração o usuário não espera encontrar as abas de volta.
                SessionStore.Clear();
            }
            else
            {
                SessionStore.Save(CaptureSession());
            }
        }

        if (!_isPrivate)
        {
            _history.Save();
            _favorites.Save();
            _settings.Save();
        }
        _downloads.Dispose();

        foreach (var tab in _tabs.ToList())
        {
            _tabStrip.Controls.Remove(tab.Header);
            _pageHost.Controls.Remove(tab.View);
            tab.View.Dispose();
            tab.SelectButton.Image = null;
            tab.Favicon?.Dispose();
            tab.ContextMenu.Dispose();
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
        public BrowserTab(WebView2 view, bool isPinned)
        {
            View = view;
            IsPinned = isPinned;

            Header = new RoundedPanel(12)
            {
                Width = isPinned ? 58 : 238,
                Height = 38,
                Margin = new Padding(4, 5, 0, 0),
                BackColor = Theme.TitleBar
            };

            SelectButton = new Button
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.TitleBar,
                ForeColor = Theme.Text,
                Font = Theme.Ui(9.25F),
                Text = "Nova guia",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                AutoEllipsis = true,
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
        public ContextMenuStrip ContextMenu { get; set; } = null!;
        public Image? Favicon { get; set; }
        public bool IsPinned { get; set; }
        public DateTimeOffset LastNavigationStartedAt { get; set; }
        public bool IsInternalNewTab { get; set; }
    }
}
