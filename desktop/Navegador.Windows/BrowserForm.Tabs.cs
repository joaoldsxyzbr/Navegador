using CefSharp;
using CefSharp.WinForms;
using CefSharp.WinForms.Handler;
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

    private Task<BrowserTab?> AddTabAsync(string? initialAddress = null, bool activate = true, bool pinned = false)
    {
        var isNewTab = string.IsNullOrWhiteSpace(initialAddress);
        var target = isNewTab
            ? null
            : AddressResolver.IsPersistable(initialAddress) ? initialAddress! : AddressResolver.Resolve(initialAddress!);
        var view = isNewTab || string.IsNullOrWhiteSpace(target)
            ? new ChromiumWebBrowser(new HtmlString(NewTabPage.Build(_favorites.Items, _isPrivate)), _requestContext)
            : new ChromiumWebBrowser(target, _requestContext);
        view.Dock = DockStyle.Fill;
        view.Visible = false;

        var tab = new BrowserTab(view, pinned) { IsInternalNewTab = isNewTab || string.IsNullOrWhiteSpace(target) };
        view.DownloadHandler = _downloads.Handler;
        view.DisplayHandler = new BrowserFaviconHandler(urls =>
        {
            var faviconUrl = urls.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(faviconUrl))
            {
                PostToUi(() => SetTabFavicon(tab, null));
                return;
            }

            _ = UpdateTabFaviconAsync(tab, faviconUrl);
        });
        view.LifeSpanHandler = new LifeSpanHandler().OnBeforePopupCreated(
            (_, _, _, targetUrl, _, _, _, _) =>
            {
                if (targetUrl?.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase) == true)
                    return PopupCreation.Continue;

                if (!string.IsNullOrWhiteSpace(targetUrl)) PostToUi(() => OpenUrlInNewTab(targetUrl));
                return PopupCreation.Cancel;
            });
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

        AttachBrowserEvents(tab);
        _toolTip.SetToolTip(tab.SelectButton, tab.IsInternalNewTab ? "Nova guia" : target ?? string.Empty);
        return Task.FromResult<BrowserTab?>(tab);
    }

    /// <summary>Uma aba pode estar fechando: antes de agir, confirme que ela vive.</summary>
    private bool IsAlive(BrowserTab tab) => !tab.View.IsDisposed && _tabs.Contains(tab);

    private bool PostToUi(Action action)
    {
        if (IsDisposed || Disposing || !IsHandleCreated) return false;

        try
        {
            if (InvokeRequired) BeginInvoke(action);
            else action();
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException)
        {
            // O Chromium pode emitir um último evento enquanto a janela está fechando.
            return false;
        }
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

        _address.Text = tab.IsInternalNewTab ? string.Empty : tab.View.Address ?? string.Empty;

        UpdateNavigationButtons(tab);
        RefreshBookmarkButton();
        UpdateWindowTitle();
    }

    private void CloseTab(BrowserTab tab)
    {
        var index = _tabs.IndexOf(tab);
        if (index < 0) return;

        var url = tab.View.Address;
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

    private static void SetTabFavicon(BrowserTab tab, Image? favicon)
    {
        if (ReferenceEquals(tab.Favicon, favicon)) return;
        var previous = tab.Favicon;
        tab.Favicon = favicon;
        tab.SelectButton.Image = favicon;
        previous?.Dispose();
    }

    private async Task UpdateTabFaviconAsync(BrowserTab tab, string faviconUrl)
    {
        try
        {
            Uri? uri;
            if (!Uri.TryCreate(faviconUrl, UriKind.Absolute, out uri))
            {
                if (!Uri.TryCreate(tab.View.Address, UriKind.Absolute, out var pageUri) ||
                    !Uri.TryCreate(pageUri, faviconUrl, out uri)) return;
            }

            using var response = await FaviconClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength is > 2_000_000) return;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length is 0 or > 2_000_000) return;

            using var stream = new MemoryStream(bytes);
            using var image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true);
            var favicon = new Bitmap(image, new Size(18, 18));

            if (!PostToUi(() =>
            {
                if (!IsAlive(tab)) favicon.Dispose();
                else SetTabFavicon(tab, favicon);
            })) favicon.Dispose();
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or IOException or ArgumentException or OutOfMemoryException or
                System.Runtime.InteropServices.ExternalException)
        {
            // Algumas páginas retiram o favicon antes de a imagem ficar disponível.
        }
    }

    private void UpdateTabTitle(BrowserTab tab)
    {
        var title = tab.IsInternalNewTab ? "Nova guia" : tab.Title;
        var displayTitle = string.IsNullOrWhiteSpace(title) ? "Nova guia" : title;
        tab.SelectButton.Text = tab.IsPinned ? string.Empty : Shorten(displayTitle, 28);
        _toolTip.SetToolTip(tab.SelectButton, displayTitle);

        if (tab == _activeTab) UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        var title = _activeTab?.Title;

        var pageTitle = string.IsNullOrWhiteSpace(title) || title == "Nova guia"
            ? AppName
            : $"{Shorten(title, 60)} — {AppName}";
        Text = _isPrivate ? $"Navegação privada — {pageTitle}" : pageTitle;
    }

    private void UpdateNavigationButtons(BrowserTab tab)
    {
        SetNavigationState(_backButton, tab.View.CanGoBack);
        SetNavigationState(_forwardButton, tab.View.CanGoForward);
        SetNavigationState(_reloadButton, tab.View.IsBrowserInitialized);
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
            var url = tab.View.Address;
            if (!AddressResolver.IsPersistable(url)) continue;

            if (tab == _activeTab) snapshot.ActiveIndex = snapshot.Tabs.Count;

            snapshot.Tabs.Add(new SessionTab
            {
                Url = url!,
                Title = tab.Title ?? url!,
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

        _requestContext?.Dispose();
    }

    private static string Shorten(string value, int length)
    {
        var single = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return single.Length <= length ? single : single[..(length - 1)] + "…";
    }

    private sealed class BrowserTab
    {
        public BrowserTab(ChromiumWebBrowser view, bool isPinned)
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

        public ChromiumWebBrowser View { get; }

        public string? Title { get; set; }

        public RoundedPanel Header { get; }

        public Button SelectButton { get; }

        public Button CloseButton { get; }
        public ContextMenuStrip ContextMenu { get; set; } = null!;
        public Image? Favicon { get; set; }
        public bool IsPinned { get; set; }
        public DateTimeOffset LastNavigationStartedAt { get; set; }
        public bool IsInternalNewTab { get; set; }
    }

    private sealed class BrowserFaviconHandler(Action<IList<string>> onFaviconUrlsChanged) : CefSharp.Handler.DisplayHandler
    {
        protected override void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> urls) =>
            onFaviconUrlsChanged(urls);
    }
}
