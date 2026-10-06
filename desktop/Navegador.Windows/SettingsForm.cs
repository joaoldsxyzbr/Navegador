using Navegador.Core;
using Navegador.Core.Storage;
using Navegador.Core.Updates;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

/// <summary>Preferências do usuário e informações do perfil do Rumo.</summary>
internal sealed class SettingsForm : Form
{
    private readonly SettingsStore _settings;
    private readonly FlowLayoutPanel _sections = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = Theme.TitleBar,
        Padding = new Padding(3, 6, 12, 12),
        Margin = Padding.Empty
    };
    private readonly TextBox _downloadFolder = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        AccessibleName = "Pasta de downloads"
    };
    private readonly TextBox _homeUrl = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        AccessibleName = "Página inicial"
    };
    private readonly CheckBox _restoreSession = new()
    {
        Text = "Reabrir minhas abas quando o Rumo iniciar",
        AutoSize = true,
        AccessibleName = "Reabrir as abas da última sessão"
    };
    private readonly CheckBox _askWhereToSave = new()
    {
        Text = "Perguntar onde salvar cada download",
        AutoSize = true,
        AccessibleName = "Perguntar onde salvar antes de cada download"
    };
    private readonly ToolTip _toolTip = new();

    public SettingsForm(SettingsStore settings)
    {
        _settings = settings;

        Text = $"Configurações do {Branding.Name}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(720, 660);
        ClientSize = new Size(820, 760);
        BackColor = Theme.TitleBar;
        ForeColor = Theme.Text;
        Font = Theme.Ui(10F);
        AutoScaleMode = AutoScaleMode.Font;
        MaximizeBox = false;
        MinimizeBox = false;

        foreach (var box in new[] { _downloadFolder, _homeUrl })
        {
            box.BackColor = Theme.Address;
            box.ForeColor = Theme.Text;
            box.Font = Theme.Ui(9.5F);
            box.Margin = new Padding(0, 2, 0, 2);
            box.AutoSize = false;
            box.Height = 32;
        }

        foreach (var check in new[] { _restoreSession, _askWhereToSave })
        {
            check.ForeColor = Theme.Text;
            check.BackColor = Theme.ActiveTab;
            check.FlatStyle = FlatStyle.Flat;
            check.Font = Theme.Ui(9.5F);
            check.Margin = Padding.Empty;
        }

        _restoreSession.Checked = settings.Current.RestoreSession;
        _askWhereToSave.Checked = settings.Current.AskWhereToSaveDownloads;
        _downloadFolder.Text = settings.Current.DownloadFolder;
        _homeUrl.Text = settings.Current.HomeUrl;

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.TitleBar,
            Padding = new Padding(24, 15, 24, 0),
            Margin = Padding.Empty
        };
        header.Controls.Add(new Label
        {
            Text = "Configurações",
            Dock = DockStyle.Top,
            Height = 32,
            Font = Theme.Ui(17F),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        });
        header.Controls.Add(new Label
        {
            Text = "Personalize como o Rumo abre páginas e salva seus arquivos.",
            Dock = DockStyle.Bottom,
            Height = 28,
            Font = Theme.Ui(9F),
            ForeColor = Theme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        });

        BuildSections();
        _sections.SizeChanged += (_, _) => ResizeCards();

        var save = MakeButton("Salvar", primary: true);
        save.DialogResult = DialogResult.OK;
        save.MinimumSize = new Size(106, 38);

        var cancel = MakeButton("Cancelar");
        cancel.DialogResult = DialogResult.Cancel;
        cancel.MinimumSize = new Size(106, 38);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Height = 56,
            Padding = new Padding(0, 8, 22, 6),
            BackColor = Theme.TitleBar,
            Margin = Padding.Empty
        };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.TitleBar,
            Margin = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_sections, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);

        AcceptButton = save;
        CancelButton = cancel;
    }

    /// <summary>Aplica o que o usuário escolheu. Devolve true quando a sessão foi alterada.</summary>
    public bool Apply()
    {
        var settings = _settings.Current;
        var previousRestore = settings.RestoreSession;

        settings.RestoreSession = _restoreSession.Checked;
        settings.AskWhereToSaveDownloads = _askWhereToSave.Checked;
        settings.DownloadFolder = _downloadFolder.Text.Trim();
        settings.HomeUrl = string.IsNullOrWhiteSpace(_homeUrl.Text)
            ? "https://www.google.com/"
            : _homeUrl.Text.Trim();

        _settings.Save();

        return previousRestore != settings.RestoreSession;
    }

    private void BuildSections()
    {
        var sessionCard = CreateCard(
            "Ao iniciar",
            "Escolha se o navegador deve restaurar as páginas da última vez.",
            116,
            out var sessionContent);
        _restoreSession.Dock = DockStyle.Fill;
        sessionContent.Controls.Add(_restoreSession);
        _sections.Controls.Add(sessionCard);

        var homeCard = CreateCard(
            "Página inicial",
            "Use um endereço da web ou deixe vazio para usar o Google.",
            138,
            out var homeContent);
        homeContent.Controls.Add(_homeUrl);
        _sections.Controls.Add(homeCard);

        var downloadsCard = CreateCard(
            "Downloads",
            "Defina onde os arquivos serão guardados.",
            180,
            out var downloadsContent);
        var downloadsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Theme.ActiveTab,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        downloadsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        downloadsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
        downloadsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        downloadsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var browse = MakeButton("Escolher pasta");
        browse.Dock = DockStyle.Fill;
        browse.Margin = new Padding(8, 2, 0, 2);
        browse.Click += (_, _) => BrowseForFolder();

        downloadsLayout.Controls.Add(_downloadFolder, 0, 0);
        downloadsLayout.Controls.Add(browse, 1, 0);
        downloadsLayout.Controls.Add(_askWhereToSave, 0, 1);
        downloadsLayout.SetColumnSpan(_askWhereToSave, 2);
        downloadsContent.Controls.Add(downloadsLayout);
        _sections.Controls.Add(downloadsCard);

        var aboutCard = CreateCard(
            "Sobre este perfil",
            "Versão do aplicativo e local onde os dados ficam guardados.",
            184,
            out var aboutContent);
        var info = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            BackColor = Theme.ActiveTab,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106));
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var index = 0; index < 4; index++)
            info.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        AddInfoRow(info, 0, "Versão", CurrentVersion.Display);
        AddInfoRow(info, 1, "Motor", $"WebView2 {WebViewVersion()}");
        AddInfoRow(info, 2, "Modo", AppPaths.IsPortable ? "Portátil" : "Perfil do usuário");
        AddInfoRow(info, 3, "Dados", AppPaths.DataDirectory);
        aboutContent.Controls.Add(info);
        _toolTip.SetToolTip(info.GetControlFromPosition(1, 3)!, AppPaths.DataDirectory);
        _sections.Controls.Add(aboutCard);

        ResizeCards();
    }

    private RoundedPanel CreateCard(
        string title,
        string description,
        int height,
        out Panel content)
    {
        var card = new RoundedPanel(12)
        {
            Width = GetCardWidth(),
            Height = height,
            BackColor = Theme.ActiveTab,
            BorderColor = Theme.AddressBorder,
            BorderWidth = 1F,
            Padding = new Padding(16, 11, 16, 11),
            Margin = new Padding(0, 0, 0, 14)
        };

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.ActiveTab,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = Theme.Ui(10F),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        header.Controls.Add(new Label
        {
            Text = description,
            Dock = DockStyle.Fill,
            Font = Theme.Ui(8.5F),
            ForeColor = Theme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 1);

        content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.ActiveTab,
            Margin = Padding.Empty
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.ActiveTab,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(content, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static void AddInfoRow(TableLayoutPanel layout, int row, string caption, string value)
    {
        layout.Controls.Add(new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            ForeColor = Theme.MutedText,
            Font = Theme.Ui(8.5F),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        layout.Controls.Add(new Label
        {
            Text = value,
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            Font = Theme.Ui(8.5F),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            AccessibleName = $"{caption}: {value}"
        }, 1, row);
    }

    private Button MakeButton(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 36,
            MinimumSize = new Size(96, 36),
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

    private void ResizeCards()
    {
        var width = GetCardWidth();
        foreach (Control card in _sections.Controls)
        {
            if (card.Width != width) card.Width = width;
        }
    }

    private int GetCardWidth() =>
        Math.Max(540, _sections.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 22);

    private void BrowseForFolder()
    {
        using var picker = new FolderBrowserDialog
        {
            Description = "Escolha a pasta de downloads",
            UseDescriptionForTitle = true,
            SelectedPath = _downloadFolder.Text
        };

        if (picker.ShowDialog(this) == DialogResult.OK) _downloadFolder.Text = picker.SelectedPath;
    }

    private static string WebViewVersion()
    {
        try
        {
            var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
            return string.IsNullOrWhiteSpace(version) ? "não instalado" : version;
        }
        catch (Exception exception) when (
            exception is Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException or InvalidOperationException)
        {
            return "não instalado";
        }
    }
}
