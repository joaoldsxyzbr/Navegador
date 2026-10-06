using Microsoft.Web.WebView2.Core;
using Navegador.Core;
using Navegador.Core.Models;
using Navegador.Core.Storage;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

internal sealed partial class BrowserForm
{
    /// <summary>Grava o histórico a cada N visitas para não escrever em disco demais.</summary>
    private const int HistoryFlushEvery = 20;

    private int _visitsSinceHistoryFlush;

    private void AttachBrowserEvents(BrowserTab tab)
    {
        var core = tab.View.CoreWebView2!;

        core.NavigationStarting += (_, eventArgs) =>
        {
            tab.LastNavigationStartedAt = DateTimeOffset.UtcNow;
            if (tab.IsInternalNewTab && !NewTabPage.IsInternalSource(eventArgs.Uri)) tab.IsInternalNewTab = false;
            SetTabFavicon(tab, null);
            if (tab == _activeTab) _address.Text = tab.IsInternalNewTab ? string.Empty : eventArgs.Uri;
            UpdateNavigationButtons(tab);
        };

        core.SourceChanged += (_, _) =>
        {
            if (tab == _activeTab) _address.Text = tab.IsInternalNewTab ? string.Empty : core.Source;
        };

        core.FaviconChanged += async (_, _) => await UpdateTabFaviconAsync(tab);

        core.DocumentTitleChanged += (_, _) =>
        {
            UpdateTabTitle(tab);
            UpdateHistoryTitle(tab);
        };

        core.NavigationCompleted += (_, _) =>
        {
            UpdateTabTitle(tab);
            UpdateNavigationButtons(tab);
            RecordVisit(tab);

            if (tab == _activeTab)
            {
                _address.Text = tab.IsInternalNewTab ? string.Empty : core.Source;
                RefreshBookmarkButton();
            }
        };

        core.WebMessageReceived += (_, eventArgs) =>
        {
            if (!tab.IsInternalNewTab) return;
            if (!NewTabPage.TryGetNavigationTarget(eventArgs.WebMessageAsJson, out var input)) return;
            var target = AddressResolver.Resolve(input);
            if (string.IsNullOrWhiteSpace(target)) return;
            tab.IsInternalNewTab = false;
            core.Navigate(target);
        };

        core.NewWindowRequested += (_, eventArgs) =>
        {
            // Janelas de popup viram guias novas, como no Chrome.
            eventArgs.Handled = true;
            _ = AddTabAsync(eventArgs.Uri);
        };

        core.DownloadStarting += (_, eventArgs) =>
        {
            _downloads.Begin(eventArgs);
            _downloadsBar.Reveal();
        };

        core.ProcessFailed += (_, eventArgs) =>
        {
            if (eventArgs.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
            {
                MessageBox.Show(
                    this,
                    "O processo que renderiza as páginas foi encerrado. Recarregue a guia para continuar.",
                    AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        };
    }

    private void NavigateAddress()
    {
        if (_addressSuggestions.Visible && _addressSuggestions.SelectedItem is AddressSuggestion suggestion)
        {
            NavigateAddress(suggestion.Url);
            return;
        }

        NavigateAddress(_address.Text);
    }

    private void NavigateAddress(string input)
    {
        input = input.Trim();
        _addressSuggestions.Visible = false;
        if (input.Length == 0 || ActiveCore is not { } core) return;

        if (!string.Equals(_address.Text, input, StringComparison.Ordinal)) _address.Text = input;
        core.Navigate(AddressResolver.Resolve(input));
    }

    private void NavigateSelectedSuggestion()
    {
        if (_addressSuggestions.SelectedItem is AddressSuggestion suggestion)
            NavigateAddress(suggestion.Url);
    }

    private void RefreshAddressSuggestions()
    {
        if (!_address.Focused || _isFullscreen || string.IsNullOrWhiteSpace(_address.Text))
        {
            _addressSuggestions.Visible = false;
            return;
        }

        IEnumerable<HistoryEntry> history = _isPrivate ? Enumerable.Empty<HistoryEntry>() : _history.Items;
        var suggestions = AddressSuggestionResolver.Suggest(_address.Text, _favorites.Items, history);
        _addressSuggestions.BeginUpdate();
        _addressSuggestions.Items.Clear();
        foreach (var suggestion in suggestions) _addressSuggestions.Items.Add(suggestion);
        _addressSuggestions.EndUpdate();

        if (suggestions.Count == 0)
        {
            _addressSuggestions.Visible = false;
            return;
        }

        _addressSuggestions.SelectedIndex = -1;
        var location = PointToClient(_addressShell.PointToScreen(new Point(0, _addressShell.Height + 2)));
        var availableHeight = ClientSize.Height - location.Y - 8;
        if (availableHeight < _addressSuggestions.ItemHeight)
        {
            _addressSuggestions.Visible = false;
            return;
        }

        _addressSuggestions.Location = location;
        _addressSuggestions.Width = _addressShell.Width;
        _addressSuggestions.Height = Math.Min(suggestions.Count * _addressSuggestions.ItemHeight, availableHeight);
        _addressSuggestions.Visible = true;
        _addressSuggestions.BringToFront();
    }

    private void DrawAddressSuggestion(object? sender, DrawItemEventArgs eventArgs)
    {
        if (eventArgs.Index < 0 || eventArgs.Index >= _addressSuggestions.Items.Count) return;

        var suggestion = (AddressSuggestion)_addressSuggestions.Items[eventArgs.Index]!;
        var selected = (eventArgs.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(selected ? HoverColor : ActiveTabColor);
        eventArgs.Graphics.FillRectangle(background, eventArgs.Bounds);

        var titleFont = _addressSuggestions.Font;
        using var detailFont = new Font(titleFont.FontFamily, 8F, FontStyle.Regular);
        var titleBounds = new Rectangle(eventArgs.Bounds.X + 12, eventArgs.Bounds.Y + 5, eventArgs.Bounds.Width - 24, 19);
        var detailBounds = new Rectangle(eventArgs.Bounds.X + 12, eventArgs.Bounds.Y + 25, eventArgs.Bounds.Width - 24, 17);
        TextRenderer.DrawText(eventArgs.Graphics, suggestion.Title, titleFont, titleBounds, TextColor,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        var detail = suggestion.IsFavorite ? $"Favorito  ·  {suggestion.Url}" : suggestion.Url;
        TextRenderer.DrawText(eventArgs.Graphics, detail, detailFont, detailBounds, Theme.MutedText,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        eventArgs.DrawFocusRectangle();
    }

    private void NavigateBack()
    {
        if (ActiveCore?.CanGoBack == true) ActiveCore.GoBack();
    }

    private void NavigateForward()
    {
        if (ActiveCore?.CanGoForward == true) ActiveCore.GoForward();
    }

    private void ReloadActiveTab()
    {
        if (ActiveCore is { } core) core.Reload();
    }

    private void StopOrReload()
    {
        if (ActiveCore is not { } core) return;

        core.Stop();
        core.Reload();
    }

    /// <summary>Registra a visita no histórico e mantém o arquivo atualizado.</summary>
    private void RecordVisit(BrowserTab tab)
    {
        if (_isPrivate || !IsAlive(tab)) return;
        if (_historyClearedAt is { } clearedAt && tab.LastNavigationStartedAt <= clearedAt) return;

        var core = tab.View.CoreWebView2;
        var url = core?.Source;

        if (!AddressResolver.IsPersistable(url)) return;

        var title = core?.DocumentTitle;
        _history.Record(url!, title);

        if (++_visitsSinceHistoryFlush < HistoryFlushEvery) return;

        _visitsSinceHistoryFlush = 0;
        _history.Save();
    }

    private void UpdateHistoryTitle(BrowserTab tab)
    {
        if (_isPrivate || !IsAlive(tab)) return;
        var core = tab.View.CoreWebView2;
        var url = core?.Source;
        var title = core?.DocumentTitle;
        if (!AddressResolver.IsPersistable(url) || string.IsNullOrWhiteSpace(title)) return;
        _history.UpdateTitle(url!, title);
    }

    private void ToggleFavoriteForActiveTab()
    {
        if (_isPrivate) return;
        var url = ActiveCore?.Source;
        if (!AddressResolver.IsPersistable(url))
        {
            MessageBox.Show(this, "Abra uma página para adicionar aos favoritos.", "Favoritos",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var title = ActiveCore?.DocumentTitle;
        var added = _favorites.Add(url!, title);

        RefreshBookmarkButton();
        RefreshFavoritesBar();

        _toolTip.SetToolTip(_bookmarkButton, added ? "Remover dos favoritos" : "Adicionar aos favoritos");
    }

    private void RefreshBookmarkButton()
    {
        var url = ActiveCore?.Source;
        var saved = _favorites.Contains(url);

        _bookmarkButton.Icon = saved ? BrowserIcon.StarFilled : BrowserIcon.Star;
        _bookmarkButton.ForeColor = saved ? Theme.Starred : TextColor;
        _bookmarkButton.Invalidate();
        _toolTip.SetToolTip(_bookmarkButton, saved ? "Remover dos favoritos" : "Adicionar aos favoritos");
    }

    /// <summary>Redesenha a barra de favoritos; ela some quando não há favoritos.</summary>
    private void RefreshFavoritesBar()
    {
        _favoritesBar.SuspendLayout();

        foreach (Control control in _favoritesBar.Controls) control.Dispose();
        _favoritesBar.Controls.Clear();

        foreach (var favorite in _favorites.Items.Take(40))
        {
            var button = new Button
            {
                Text = Shorten(favorite.Title, 20),
                AutoSize = true,
                Height = 26,
                Margin = new Padding(2, 3, 2, 3),
                Padding = new Padding(8, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = ToolbarColor,
                ForeColor = TextColor,
                Font = Theme.Ui(9F),
                UseVisualStyleBackColor = false,
                TabStop = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = HoverColor;
            button.FlatAppearance.MouseDownBackColor = PressColor;

            var url = favorite.Url;
            button.Click += (_, _) => OpenUrlInActiveTab(url);
            _toolTip.SetToolTip(button, favorite.Url);

            _favoritesBar.Controls.Add(button);
        }

        _favoritesBar.ResumeLayout();

        _favoritesRowStyle.Height = _favorites.Count > 0 ? 32 : 0;
        _favoritesBar.Visible = _favorites.Count > 0;
    }

    /// <summary>Abre a URL na aba ativa, criando uma se a janela estiver vazia.</summary>
    private void OpenUrlInActiveTab(string url)
    {
        if (ActiveCore is { } core)
        {
            core.Navigate(url);
            return;
        }

        _ = AddTabAsync(url);
    }

    private void OpenUrlInNewTab(string url) => _ = AddTabAsync(url);

    /// <summary>Endereço e título da aba ativa, para os favoritos.</summary>
    private (string Url, string Title)? CurrentPage()
    {
        var core = ActiveCore;
        if (core?.Source is not { } url || !AddressResolver.IsPersistable(url)) return null;

        return (url, core.DocumentTitle ?? url);
    }

    private void OpenExtensions()
    {
        var profile = ActiveCore?.Profile;
        if (profile is null)
        {
            MessageBox.Show(
                this,
                "Aguarde a guia terminar de iniciar para gerenciar extensões.",
                "Extensões",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var dialog = new ExtensionsForm(profile);
        dialog.ShowDialog(this);
    }

    private void ShowFavorites()
    {
        using var dialog = new FavoritesForm(_favorites, OpenUrlInActiveTab, CurrentPage);
        dialog.ShowDialog(this);

        RefreshFavoritesBar();
        RefreshBookmarkButton();
    }

    private void ShowHistory()
    {
        using var dialog = new HistoryForm(_history, OpenUrlInActiveTab);
        dialog.ShowDialog(this);
    }

    private void ShowDownloads()
    {
        _downloadsBar.Reveal();

        using var dialog = new DownloadsForm(_downloads, _settings);
        dialog.ShowDialog(this);
    }

    private void ShowSettings()
    {
        using var dialog = new SettingsForm(_settings);

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        dialog.Apply();
        RefreshFavoritesBar();
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            this,
            $"{AppName} {Navegador.Core.Updates.CurrentVersion.Display}\n\n" +
            $"Motor: Microsoft Edge WebView2\n" +
            $"Dados: {AppPaths.DataDirectory}\n" +
            $"Modo: {(AppPaths.IsPortable ? "portátil" : "perfil do usuário")}\n\n" +
            "Extensões são carregadas de pastas locais descompactadas; a Chrome Web Store\n" +
            "e as janelas de popup das extensões ainda não são suportadas.",
            $"Sobre o {AppName}",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
