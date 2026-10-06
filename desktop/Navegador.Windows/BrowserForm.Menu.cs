using Navegador.Core.Storage;
using Navegador.Windows.Ui;

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
        var favoritesItem = Item("Favoritos", "Ctrl+Shift+O", () =>
        {
            ShowFavorites();
            RefreshMenuCounts();
        });
        var historyItem = Item("Histórico", "Ctrl+H", ShowHistory);

        _downloadsMenuItem = Item("Downloads", "Ctrl+J", ShowDownloads);

        var extensionsItem = Item("Extensões", null, OpenExtensions);
        var settingsItem = Item("Configurações", null, ShowSettings);
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
        // Atalhos iguais aos do Chrome para o que o Navegador já faz.
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
                ShowHistory();
                return true;

            case Keys.Control | Keys.J:
                ShowDownloads();
                return true;

            case Keys.Control | Keys.Shift | Keys.O:
                ShowFavorites();
                return true;

            case Keys.Control | Keys.Shift | Keys.T:
                ReopenClosedTab();
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
