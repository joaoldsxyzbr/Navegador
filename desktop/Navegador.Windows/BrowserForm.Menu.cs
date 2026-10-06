using Navegador.Core;
using Navegador.Core.Storage;
using Navegador.Windows.Ui;
using CefSharp;

namespace Navegador.Windows;

internal sealed partial class BrowserForm
{
    private ToolStripMenuItem _downloadsMenuItem = null!;
    private ToolStripMenuItem _reopenClosedTabMenuItem = null!;
    private ToolStripMenuItem _updateMenuItem = null!;
    private bool _checkingForUpdates;

    private ContextMenuStrip BuildBrowserMenu()
    {
        var menu = new ContextMenuStrip
        {
            BackColor = ActiveTabColor,
            ForeColor = TextColor,
            ShowImageMargin = false,
            Font = Theme.Ui(10F),
            Padding = new Padding(4)
        };

        var newTabItem = Item("Nova guia", "Ctrl+T", async () => await AddTabAsync());
        _reopenClosedTabMenuItem = Item("Reabrir guia fechada", "Ctrl+Shift+T", ReopenClosedTab);
        var bookmarkItem = Item("Adicionar aos favoritos", "Ctrl+D", () =>
        {
            ToggleFavoriteForActiveTab();
            RefreshMenuCounts();
        });
        bookmarkItem.Enabled = !_isPrivate;
        var favoritesItem = Item("Favoritos", "Ctrl+Shift+O", () =>
        {
            ShowFavorites();
            RefreshMenuCounts();
        });
        favoritesItem.Enabled = !_isPrivate;
        var historyItem = Item("Histórico", "Ctrl+H", ShowHistory);
        historyItem.Enabled = !_isPrivate;
        var privateWindowItem = Item("Nova janela privada", "Ctrl+Shift+N", OpenPrivateWindow);
        var clearDataItem = Item("Limpar dados de navegação…", null, () => _ = ClearBrowsingDataAsync());

        _downloadsMenuItem = Item("Downloads", "Ctrl+J", ShowDownloads);

        var extensionsItem = Item("Extensões", null, OpenExtensions);
        var settingsItem = Item("Configurações", null, ShowSettings);
        settingsItem.Enabled = !_isPrivate;
        _updateMenuItem = Item($"Atualizar {AppName}", null, async () => await CheckForUpdatesAsync());
        var aboutItem = Item($"Sobre o {AppName}", null, ShowAbout);
        var closeTabItem = Item("Fechar guia", "Ctrl+W", CloseActiveTab);

        menu.Items.AddRange(
        [
            newTabItem,
            _reopenClosedTabMenuItem,
            bookmarkItem,
            favoritesItem,
            historyItem,
            privateWindowItem,
            clearDataItem,
            _downloadsMenuItem,
            extensionsItem,
            new ToolStripSeparator(),
            settingsItem,
            _updateMenuItem,
            aboutItem,
            new ToolStripSeparator(),
            closeTabItem
        ]);

        menu.Opening += (_, _) => RefreshMenuCounts();
        return menu;
    }

    private ToolStripMenuItem Item(string text, string? shortcut, Action action)
    {
        var item = new ToolStripMenuItem(shortcut is null ? text : $"{text}\t{shortcut}")
        {
            BackColor = ActiveTabColor,
            ForeColor = TextColor
        };

        item.Click += (_, _) => action();
        return item;
    }

    private void RefreshMenuCounts()
    {
        _downloadsMenuItem.Text = _downloads.Items.Count == 0
            ? "Downloads\tCtrl+J"
            : $"Downloads ({_downloads.Items.Count(item => item.IsRunning)} ativos)\tCtrl+J";

        _reopenClosedTabMenuItem.Enabled = _closedTabs.Count > 0;
        _updateMenuItem.Enabled = !_checkingForUpdates;
        _updateButton.Enabled = !_checkingForUpdates;
    }

    private void OpenPrivateWindow()
    {
        var window = new BrowserForm(isPrivate: true);
        window.Show(this);
    }

    private async Task ClearBrowsingDataAsync()
    {
        var requestContext = ActiveCore?.RequestContext;
        if (requestContext is null)
        {
            MessageBox.Show(this, "Aguarde a guia terminar de iniciar para limpar os dados.", AppName,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var details = _isPrivate
            ? "Isso apagará cookies, cache e dados de sites desta janela privada. O histórico normal, os favoritos e os arquivos baixados serão mantidos."
            : "Isso apagará cookies, cache, dados de sites e o histórico do Rumo. Favoritos e arquivos baixados serão mantidos; alguns sites pedirão login novamente.";
        var answer = MessageBox.Show(this, details + "\n\nDeseja continuar?", "Limpar dados de navegação",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes) return;

        try
        {
            var completion = new TaskCompletionCallback();
            requestContext.ClearHttpCache(completion);

            var cookies = requestContext.GetCookieManager(null);
            TaskCompletionCallback? cookieFlush = null;
            if (cookies is not null)
            {
                cookies.DeleteCookies(string.Empty, string.Empty, null);
                cookieFlush = new TaskCompletionCallback();
                cookies.FlushStore(cookieFlush);
            }

            if (!await completion.Task)
                throw new InvalidOperationException("O Chromium não confirmou a limpeza do cache.");
            if (cookieFlush is not null) await cookieFlush.Task;
            if (IsDisposed) return;
            if (!_isPrivate)
            {
                _history.Clear();
                _historyClearedAt = DateTimeOffset.UtcNow;
            }
            MessageBox.Show(this, "Os dados de navegação foram apagados.", "Rumo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, "Não foi possível limpar todos os dados de navegação.\n\n" + exception.Message,
                "Rumo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        if (_checkingForUpdates) return;
        _checkingForUpdates = true;
        _updateMenuItem.Enabled = false;
        _updateButton.Enabled = false;
        try { await UpdateService.CheckAndInstallAsync(this); }
        finally
        {
            _checkingForUpdates = false;
            if (!IsDisposed) { _updateMenuItem.Enabled = true; _updateButton.Enabled = true; }
        }
    }

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.F11)
        {
            ToggleFullscreen();
            return true;
        }

        if (keyData == Keys.Escape && _isFullscreen)
        {
            ToggleFullscreen();
            return true;
        }

        if (_address.Focused && _addressSuggestions.Visible)
        {
            if (keyData == Keys.Down && _addressSuggestions.Items.Count > 0)
            {
                _addressSuggestions.SelectedIndex = Math.Min(_addressSuggestions.SelectedIndex + 1,
                    _addressSuggestions.Items.Count - 1);
                return true;
            }

            if (keyData == Keys.Up && _addressSuggestions.Items.Count > 0)
            {
                _addressSuggestions.SelectedIndex = _addressSuggestions.SelectedIndex <= 0
                    ? _addressSuggestions.Items.Count - 1
                    : _addressSuggestions.SelectedIndex - 1;
                return true;
            }

            if (keyData == Keys.Enter && _addressSuggestions.SelectedItem is AddressSuggestion suggestion)
            {
                NavigateAddress(suggestion.Url);
                return true;
            }

            if (keyData == Keys.Escape)
            {
                _addressSuggestions.Visible = false;
                return true;
            }
        }

        // Atalhos iguais aos do Chrome para o que o Rumo já faz.
        switch (keyData)
        {
            case Keys.Control | Keys.L:
            case Keys.Alt | Keys.D:
                _address.Focus();
                _address.SelectAll();
                return true;

            case Keys.Control | Keys.T:
                _ = AddTabAsync();
                return true;

            case Keys.Control | Keys.W:
            case Keys.Control | Keys.F4:
                CloseActiveTab();
                return true;

            case Keys.Control | Keys.R:
            case Keys.F5:
                ReloadActiveTab();
                return true;

            case Keys.Control | Keys.D:
                ToggleFavoriteForActiveTab();
                return true;

            case Keys.Control | Keys.H:
                if (!_isPrivate) ShowHistory();
                return true;

            case Keys.Control | Keys.J:
                ShowDownloads();
                return true;

            case Keys.Control | Keys.Shift | Keys.O:
                if (!_isPrivate) ShowFavorites();
                return true;

            case Keys.Control | Keys.Shift | Keys.T:
                ReopenClosedTab();
                return true;

            case Keys.Control | Keys.Shift | Keys.N:
                OpenPrivateWindow();
                return true;

            case Keys.Alt | Keys.Left:
            case Keys.Control | Keys.OemOpenBrackets:
                NavigateBack();
                return true;

            case Keys.Alt | Keys.Right:
            case Keys.Control | Keys.OemCloseBrackets:
                NavigateForward();
                return true;

            case Keys.Control | Keys.Tab:
                ActivateNextTab(1);
                return true;

            case Keys.Control | Keys.Shift | Keys.Tab:
                ActivateNextTab(-1);
                return true;

            default:
                return base.ProcessCmdKey(ref message, keyData);
        }
    }

    private void ActivateNextTab(int offset)
    {
        if (_tabs.Count < 2 || _activeTab is null) return;

        var index = _tabs.IndexOf(_activeTab);
        var next = (index + offset + _tabs.Count) % _tabs.Count;

        ActivateTab(_tabs[next]);
    }
}
