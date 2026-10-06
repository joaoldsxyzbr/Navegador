using Microsoft.Web.WebView2.Core;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

/// <summary>Gerencia extensões locais do perfil ativo.</summary>
internal sealed class ExtensionsForm : Form
{
    private readonly CoreWebView2Profile _profile;
    private readonly FlowLayoutPanel _extensionList = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = false,
        FlowDirection = FlowDirection.TopDown,
        BackColor = Theme.TitleBar,
        Padding = new Padding(14, 8, 14, 8),
        TabStop = true,
        AccessibleName = "Extensões instaladas"
    };
    private readonly Label _status = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.MiddleRight,
        AutoEllipsis = true,
        Padding = new Padding(8, 0, 8, 0),
        AccessibleName = "Estado da lista de extensões"
    };

    public ExtensionsForm(CoreWebView2Profile profile)
    {
        _profile = profile;

        Text = $"Extensões do {Branding.Name}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(780, 520);
        ClientSize = new Size(920, 620);
        BackColor = Theme.TitleBar;
        ForeColor = Theme.Text;
        Font = Theme.Ui(10F);
        AutoScaleMode = AutoScaleMode.Font;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.TitleBar,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildNotice(), 0, 1);
        root.Controls.Add(_extensionList, 0, 2);
        root.Controls.Add(BuildFooter(), 0, 3);
        Controls.Add(root);

        _extensionList.SizeChanged += (_, _) => AdjustCardWidths();
        Shown += async (_, _) => await RefreshExtensionsAsync();
    }

    private Control BuildHeader()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(22, 11, 22, 4),
            BackColor = Theme.TitleBar
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = "Extensões",
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            Font = Theme.Ui(16F),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = "Instale e gerencie extensões neste perfil do Rumo.",
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        return layout;
    }

    private Control BuildNotice()
    {
        var notice = new RoundedPanel(12)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(18, 2, 18, 8),
            Padding = new Padding(14, 8, 14, 8),
            BackColor = Theme.Hover,
            BorderColor = Theme.AddressBorder,
            BorderWidth = 1F,
            AccessibleName = "Limites de compatibilidade das extensões"
        };
        notice.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Text =
                "O Rumo instala extensões locais já descompactadas, a partir de uma pasta com manifest.json. " +
                "A Chrome Web Store, os ícones de extensão na barra e seus pop-ups não estão disponíveis no WebView2."
        });

        return notice;
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(14, 8, 14, 8),
            BackColor = Theme.Toolbar
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Toolbar,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        var install = MakeButton("Instalar pasta local…", 188, primary: true);
        install.AccessibleName = "Instalar extensão de uma pasta local";
        install.Click += async (_, _) => await InstallAsync();

        var refresh = MakeButton("Atualizar lista", 132);
        refresh.Click += async (_, _) => await RefreshExtensionsAsync();

        actions.Controls.Add(install);
        actions.Controls.Add(refresh);
        footer.Controls.Add(actions, 0, 0);
        footer.Controls.Add(_status, 1, 0);

        return footer;
    }

    private Button MakeButton(string text, int width, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Theme.AddressFocus : Theme.Address,
            ForeColor = Theme.Text,
            Font = Theme.Ui(9.5F),
            Margin = new Padding(0, 1, 8, 0),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            TabStop = true
        };
        button.FlatAppearance.BorderColor = Theme.AddressBorder;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = Theme.Hover;
        button.FlatAppearance.MouseDownBackColor = Theme.Press;

        return button;
    }

    private async Task RefreshExtensionsAsync()
    {
        _status.Text = "Carregando extensões…";

        try
        {
            var installed = await _profile.GetBrowserExtensionsAsync();

            _extensionList.SuspendLayout();
            _extensionList.Controls.Clear();

            if (installed.Count == 0)
            {
                AddEmptyState();
            }
            else
            {
                foreach (var extension in installed)
                    _extensionList.Controls.Add(CreateExtensionCard(extension));
            }

            _extensionList.ResumeLayout(performLayout: true);
            _status.Text = installed.Count switch
            {
                0 => "Nenhuma extensão instalada",
                1 => "1 extensão instalada",
                _ => $"{installed.Count} extensões instaladas"
            };
        }
        catch (Exception exception)
        {
            _status.Text = "Não foi possível carregar a lista";
            ShowError("Não foi possível listar as extensões.", exception);
        }
    }

    private void AddEmptyState()
    {
        var empty = new RoundedPanel(12)
        {
            Width = CardWidth(),
            Height = 170,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(24),
            BackColor = Theme.Toolbar,
            BorderColor = Theme.AddressBorder,
            BorderWidth = 1F,
            AccessibleName = "Nenhuma extensão instalada"
        };
        _extensionList.Controls.Add(empty);
        empty.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleCenter,
            Text =
                "Nenhuma extensão instalada neste perfil.\n\n" +
                "Use “Instalar pasta local…” para escolher uma extensão descompactada " +
                "que contenha o arquivo manifest.json."
        });
    }

    private Control CreateExtensionCard(CoreWebView2BrowserExtension extension)
    {
        var card = new RoundedPanel(12)
        {
            Width = CardWidth(),
            Height = 96,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(12, 8, 12, 8),
            BackColor = Theme.Toolbar,
            BorderColor = Theme.AddressBorder,
            BorderWidth = 1F,
            AccessibleName = $"Extensão {extension.Name}"
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = Theme.Toolbar,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var name = new Label
        {
            Text = extension.Name,
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            Font = new Font(Theme.Ui(10F), FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            AccessibleName = $"Nome da extensão: {extension.Name}"
        };
        var id = new Label
        {
            Text = $"ID: {extension.Id}",
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            Font = Theme.Ui(8.5F),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            AccessibleDescription = extension.Id
        };
        var status = new Label
        {
            Text = extension.IsEnabled ? "Ativada" : "Desativada",
            Dock = DockStyle.Fill,
            ForeColor = extension.IsEnabled ? Theme.Accent : Theme.Text,
            Font = Theme.Ui(9.5F),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var toggle = MakeButton(extension.IsEnabled ? "Desativar" : "Ativar", 106);
        toggle.Height = 32;
        toggle.AccessibleName = $"{(extension.IsEnabled ? "Desativar" : "Ativar")} {extension.Name}";
        toggle.Click += async (_, _) => await ToggleAsync(extension);

        var remove = MakeButton("Remover", 106);
        remove.Height = 32;
        remove.AccessibleName = $"Remover {extension.Name}";
        remove.Click += async (_, _) => await RemoveAsync(extension);

        layout.Controls.Add(name, 0, 0);
        layout.Controls.Add(id, 0, 1);
        layout.Controls.Add(status, 1, 0);
        layout.SetRowSpan(status, 2);
        layout.Controls.Add(toggle, 2, 0);
        layout.Controls.Add(remove, 2, 1);
        card.Controls.Add(layout);

        return card;
    }

    private int CardWidth() => Math.Max(500, _extensionList.ClientSize.Width - 34);

    private void AdjustCardWidths()
    {
        var width = CardWidth();
        foreach (Control card in _extensionList.Controls)
            card.Width = width;
    }

    private async Task InstallAsync()
    {
        using var picker = new FolderBrowserDialog
        {
            Description = "Selecione a pasta da extensão descompactada, que contém manifest.json.",
            UseDescriptionForTitle = true
        };

        if (picker.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var extension = await _profile.AddBrowserExtensionAsync(picker.SelectedPath);
            await RefreshExtensionsAsync();
            MessageBox.Show(this, $"Extensão instalada: {extension.Name}", "Extensões",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ShowError(
                "Não foi possível instalar. Confirme se a pasta contém manifest.json e se a extensão é compatível.",
                exception);
        }
    }

    private async Task ToggleAsync(CoreWebView2BrowserExtension extension)
    {
        try
        {
            await extension.EnableAsync(!extension.IsEnabled);
            await RefreshExtensionsAsync();
        }
        catch (Exception exception)
        {
            ShowError("Não foi possível alterar o estado da extensão.", exception);
        }
    }

    private async Task RemoveAsync(CoreWebView2BrowserExtension extension)
    {
        var answer = MessageBox.Show(
            this,
            $"Remover “{extension.Name}” deste perfil?",
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
            ShowError("Não foi possível remover a extensão.", exception);
        }
    }

    private void ShowError(string message, Exception exception)
    {
        MessageBox.Show(this, $"{message}\n\n{exception.Message}", "Extensões",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
