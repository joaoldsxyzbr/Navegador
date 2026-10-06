using Microsoft.Web.WebView2.Core;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

/// <summary>Gerencia as extensões instaladas no perfil normal do WebView2.</summary>
internal sealed class ExtensionsForm : Form
{
    private readonly CoreWebView2Profile _profile;
    private readonly FlowLayoutPanel _extensionCards = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = Theme.TitleBar,
        Padding = new Padding(1, 4, 10, 4),
        Margin = Padding.Empty
    };
    private readonly TextBox _search = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Address,
        ForeColor = Theme.Text,
        Font = Theme.Ui(10F),
        PlaceholderText = "Buscar por nome ou ID",
        AccessibleName = "Buscar extensões instaladas",
        AutoSize = false,
        Margin = Padding.Empty
    };
    private readonly Label _emptyState = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Theme.MutedText,
        Font = Theme.Ui(10F),
        TextAlign = ContentAlignment.MiddleCenter,
        AccessibleRole = AccessibleRole.Status
    };
    private readonly Label _status = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Theme.MutedText,
        Font = Theme.Ui(9F),
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };
    private readonly Button _installButton;
    private readonly Button _refreshButton;
    private IReadOnlyList<CoreWebView2BrowserExtension> _installed = [];

    public ExtensionsForm(CoreWebView2Profile profile)
    {
        _profile = profile;

        Text = $"Extensões do {Branding.Name}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(720, 500);
        ClientSize = new Size(820, 620);
        BackColor = Theme.TitleBar;
        ForeColor = Theme.Text;
        Font = Theme.Ui(10F);
        AutoScaleMode = AutoScaleMode.Font;

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.TitleBar,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.Controls.Add(new Label
        {
            Text = "Extensões",
            Dock = DockStyle.Fill,
            Font = Theme.Ui(17F),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        header.Controls.Add(new Label
        {
            Text = "Gerencie o que está instalado neste perfil do Rumo.",
            Dock = DockStyle.Fill,
            Font = Theme.Ui(9F),
            ForeColor = Theme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        var compatibility = new RoundedPanel(12)
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.ActiveTab,
            BorderColor = Theme.AddressBorder,
            BorderWidth = 1F,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 10)
        };
        var compatibilityLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.ActiveTab,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        compatibilityLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        compatibilityLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        compatibilityLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        compatibilityLayout.Controls.Add(new Label
        {
            Text = "Como as extensões funcionam nesta versão",
            Dock = DockStyle.Fill,
            Font = Theme.Ui(9.5F),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        compatibilityLayout.Controls.Add(new Label
        {
            Text = "O WebView2 instala extensões Chromium a partir de pastas locais descompactadas. A Chrome Web Store e os popups/botões das extensões ainda não fazem parte da interface do Rumo.",
            Dock = DockStyle.Fill,
            Font = Theme.Ui(9F),
            ForeColor = Theme.MutedText,
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);
        compatibility.Controls.Add(compatibilityLayout);

        _installButton = MakeButton("Instalar extensão local", primary: true);
        _installButton.Click += async (_, _) => await InstallAsync();

        _refreshButton = MakeButton("Atualizar lista");
        _refreshButton.Click += async (_, _) => await RefreshExtensionsAsync();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.TitleBar,
            Padding = new Padding(0, 5, 0, 3),
            Margin = Padding.Empty
        };
        actions.Controls.Add(_installButton);
        actions.Controls.Add(_refreshButton);

        var searchShell = new RoundedPanel(10)
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Address,
            BorderColor = Theme.AddressBorder,
            BorderWidth = 1F,
            Padding = new Padding(14, 7, 14, 6),
            Margin = new Padding(0, 2, 0, 8)
        };
        searchShell.Controls.Add(_search);
        _search.TextChanged += (_, _) => RebuildCards();

        var cardsBody = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.TitleBar,
            Margin = Padding.Empty
        };
        cardsBody.Controls.Add(_extensionCards);
        cardsBody.Controls.Add(_emptyState);
        _emptyState.BringToFront();
        _extensionCards.SizeChanged += (_, _) => ResizeCards();

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.TitleBar,
            Padding = new Padding(0, 7, 0, 0),
            Margin = Padding.Empty
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.Controls.Add(_status, 0, 0);

        var close = MakeButton("Fechar");
        close.DialogResult = DialogResult.Cancel;
        footer.Controls.Add(close, 1, 0);
        CancelButton = close;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(22, 16, 22, 14),
            BackColor = Theme.TitleBar,
            Margin = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(compatibility, 0, 1);
        root.Controls.Add(actions, 0, 2);
        root.Controls.Add(searchShell, 0, 3);
        root.Controls.Add(cardsBody, 0, 4);
        root.Controls.Add(footer, 0, 5);
        Controls.Add(root);

        Shown += async (_, _) => await RefreshExtensionsAsync();
    }

    private Button MakeButton(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 36,
            MinimumSize = new Size(104, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Theme.AddressFocus : Theme.ActiveTab,
            ForeColor = Theme.Text,
            Font = Theme.Ui(9.5F),
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(0, 0, 8, 0),
            UseVisualStyleBackColor = false,
            AccessibleName = text,
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderColor = Theme.AddressBorder;
        button.FlatAppearance.MouseOverBackColor = Theme.Hover;
        return button;
    }

    private async Task RefreshExtensionsAsync()
    {
        _refreshButton.Enabled = false;
        _installButton.Enabled = false;

        try
        {
            var extensions = await _profile.GetBrowserExtensionsAsync();
            _installed = extensions
                .OrderBy(extension => extension.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            RebuildCards();

            var enabledCount = _installed.Count(extension => extension.IsEnabled);
            _status.Text = _installed.Count switch
            {
                0 => "Nenhuma extensão instalada",
                1 => $"1 extensão · {(enabledCount == 1 ? "ativada" : "desativada")}",
                _ => $"{_installed.Count} extensões · {enabledCount} ativadas"
            };
        }
        catch (Exception exception)
        {
            _status.Text = "Não foi possível carregar as extensões.";
            ShowError("Não foi possível listar as extensões.", exception);
        }
        finally
        {
            _refreshButton.Enabled = true;
            _installButton.Enabled = true;
        }
    }

    private void RebuildCards()
    {
        var query = _search.Text.Trim();
        var matches = _installed
            .Where(extension =>
                query.Length == 0 ||
                extension.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                extension.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var oldCards = _extensionCards.Controls.Cast<Control>().ToArray();
        _extensionCards.Controls.Clear();
        foreach (var oldCard in oldCards) oldCard.Dispose();

        foreach (var extension in matches)
        {
            _extensionCards.Controls.Add(CreateExtensionCard(extension));
        }

        var hasExtensions = matches.Length > 0;
        _extensionCards.Visible = hasExtensions;
        _emptyState.Visible = !hasExtensions;
        _emptyState.Text = _installed.Count == 0
            ? "Ainda não há extensões neste perfil.\nUse “Instalar extensão local” para escolher uma pasta com manifest.json."
            : $"Nenhuma extensão corresponde a “{query}”.";
        ResizeCards();
    }

    private RoundedPanel CreateExtensionCard(CoreWebView2BrowserExtension extension)
    {
        var card = new RoundedPanel(12)
        {
            Width = GetCardWidth(),
            Height = 84,
            BackColor = Theme.ActiveTab,
            BorderColor = Theme.AddressBorder,
            BorderWidth = 1F,
            Padding = new Padding(14, 10, 12, 10),
            Margin = new Padding(0, 0, 4, 10)
        };

        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.ActiveTab,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        details.Controls.Add(new Label
        {
            Text = extension.Name,
            Dock = DockStyle.Fill,
            Font = Theme.Ui(10F),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            AccessibleName = $"Nome da extensão: {extension.Name}"
        }, 0, 0);
        details.Controls.Add(new Label
        {
            Text = $"{(extension.IsEnabled ? "Ativada" : "Desativada")}  ·  ID {extension.Id}",
            Dock = DockStyle.Fill,
            Font = Theme.Ui(8.5F),
            ForeColor = Theme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            AccessibleName = $"Estado e ID da extensão {extension.Name}"
        }, 0, 1);

        var toggle = MakeButton(extension.IsEnabled ? "Desativar" : "Ativar", primary: extension.IsEnabled);
        toggle.Click += async (_, _) => await SetEnabledAsync(extension);

        var remove = MakeButton("Remover");
        remove.Click += async (_, _) => await RemoveAsync(extension);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Theme.ActiveTab,
            Margin = new Padding(8, 4, 0, 0),
            Padding = Padding.Empty
        };
        actions.Controls.Add(remove);
        actions.Controls.Add(toggle);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.ActiveTab,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(details, 0, 0);
        layout.Controls.Add(actions, 1, 0);
        card.Controls.Add(layout);
        return card;
    }

    private async Task SetEnabledAsync(CoreWebView2BrowserExtension extension)
    {
        try
        {
            await extension.EnableAsync(!extension.IsEnabled);
            await RefreshExtensionsAsync();
        }
        catch (Exception exception)
        {
            ShowError($"Não foi possível alterar o estado de {extension.Name}.", exception);
        }
    }

    private async Task RemoveAsync(CoreWebView2BrowserExtension extension)
    {
        var answer = MessageBox.Show(
            this,
            $"Remover {extension.Name} deste perfil?",
            "Remover extensão",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes) return;

        try
        {
            await extension.RemoveAsync();
            await RefreshExtensionsAsync();
        }
        catch (Exception exception)
        {
            ShowError($"Não foi possível remover {extension.Name}.", exception);
        }
    }

    private async Task InstallAsync()
    {
        using var picker = new FolderBrowserDialog
        {
            Description = "Selecione a pasta descompactada da extensão. Ela precisa conter manifest.json.",
            UseDescriptionForTitle = true
        };

        if (picker.ShowDialog(this) != DialogResult.OK) return;

        if (!File.Exists(Path.Combine(picker.SelectedPath, "manifest.json")))
        {
            MessageBox.Show(
                this,
                "Não encontrei manifest.json nessa pasta. Escolha a pasta principal da extensão descompactada.",
                "Instalar extensão",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var extension = await _profile.AddBrowserExtensionAsync(picker.SelectedPath);
            MessageBox.Show(this, $"Extensão instalada: {extension.Name}", "Extensões",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshExtensionsAsync();
        }
        catch (Exception exception)
        {
            ShowError(
                "O WebView2 não conseguiu carregar essa extensão. Confira se a pasta contém uma extensão Chromium compatível.",
                exception);
        }
    }

    private void ResizeCards()
    {
        var width = GetCardWidth();
        foreach (Control card in _extensionCards.Controls)
        {
            if (card.Width != width) card.Width = width;
        }
    }

    private int GetCardWidth() =>
        Math.Max(430, _extensionCards.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 18);

    private void ShowError(string message, Exception exception)
    {
        MessageBox.Show(this, $"{message}\n\n{exception.Message}", "Extensões",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
