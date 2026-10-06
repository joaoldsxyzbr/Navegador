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
            if (tab == _activeTab) _address.Text = eventArgs.Uri;

            UpdateNavigationButtons(tab);
        };

        core.SourceChanged += (_, _) =>
        {
            if (tab == _activeTab) _address.Text = core.Source;
        };

        core.DocumentTitleChanged += (_, _) =>
        {
            UpdateTabTitle(tab);
            RecordVisit(tab);
        };

        core.NavigationCompleted += (_, _) =>
        {
            UpdateTabTitle(tab);
            UpdateNavigationButtons(tab);
            RecordVisit(tab);

            if (tab == _activeTab)
            {
                _address.Text = core.Source;
                RefreshBookmarkButton();
            }
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
        var input = _address.Text.Trim();
        if (input.Length == 0 || ActiveCore is not { } core) return;

        core.Navigate(AddressResolver.Resolve(input));
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
        if (!IsAlive(tab)) return;

        var core = tab.View.CoreWebView2;
        var url = core?.Source;

        if (!AddressResolver.IsPersistable(url)) return;

        var title = core?.DocumentTitle;
        _history.Record(url!, title);

        if (++_visitsSinceHistoryFlush < HistoryFlushEvery) return;

        _visitsSinceHistoryFlush = 0;
        _history.Save();
    }

    private void ToggleFavoriteForActiveTab()
    {
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

        _bookmarkButton.Text = saved ? "★" : "☆";
        _bookmarkButton.ForeColor = saved ? Theme.Starred : TextColor;
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
