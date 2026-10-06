using Navegador.Core;
using Navegador.Core.Storage;
using Navegador.Core.Updates;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

/// <summary>Preferências do usuário e informações da instalação.</summary>
internal sealed class SettingsForm : Form
{
    private readonly SettingsStore _settings;
    private readonly Panel _contentViewport = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        BackColor = Theme.TitleBar
    };
    private readonly FlowLayoutPanel _sections = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Padding = new Padding(14, 8, 14, 8),
        BackColor = Theme.TitleBar
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
        Text = "Reabrir as abas da última sessão ao iniciar",
        AutoSize = true,
        UseVisualStyleBackColor = false
    };
    private readonly CheckBox _askWhereToSave = new()
    {
        Text = "Perguntar onde salvar antes de cada download",
        AutoSize = true,
        UseVisualStyleBackColor = false
    };

    public SettingsForm(SettingsStore settings)
    {
        _settings = settings;

        Text = $"Configurações do {Branding.Name}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(700, 560);
        ClientSize = new Size(760, 620);
        BackColor = Theme.TitleBar;
        ForeColor = Theme.Text;
        Font = Theme.Ui(10F);
        AutoScaleMode = AutoScaleMode.Font;

        foreach (var box in new[] { _downloadFolder, _homeUrl })
        {
            box.BackColor = Theme.Address;
            box.ForeColor = Theme.Text;
            box.Font = Theme.Ui(10F);
            box.Height = 34;
        }

        foreach (var check in new[] { _restoreSession, _askWhereToSave })
        {
            check.ForeColor = Theme.Text;
            check.BackColor = Theme.Toolbar;
            check.FlatStyle = FlatStyle.Flat;
            check.AccessibleRole = AccessibleRole.CheckButton;
        }

        _restoreSession.Checked = settings.Current.RestoreSession;
        _askWhereToSave.Checked = settings.Current.AskWhereToSaveDownloads;
        _downloadFolder.Text = settings.Current.DownloadFolder;
        _homeUrl.Text = settings.Current.HomeUrl;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = Padding.Empty,
            BackColor = Theme.TitleBar
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(_contentViewport, 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);
        Controls.Add(root);

        var browse = MakeButton("Escolher…", 118);
        browse.Click += (_, _) => BrowseForFolder();

        _sections.Controls.Add(BuildStartupSection());
        _sections.Controls.Add(BuildDownloadsSection(browse));
        _sections.Controls.Add(BuildProfileSection());
        _contentViewport.Controls.Add(_sections);

        _contentViewport.SizeChanged += (_, _) => AdjustSectionWidths();
        Shown += (_, _) => AdjustSectionWidths();
    }

    /// <summary>Aplica o que o usuário escolheu. Devolve true quando a sessão mudou.</summary>
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

    private Control BuildHeader()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(22, 12, 22, 4),
            BackColor = Theme.TitleBar
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = "Configurações",
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            Font = Theme.Ui(16F),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = "Ajuste como o Rumo abre e salva seus arquivos.",
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        return layout;
    }

    private Control BuildStartupSection()
    {
        var card = CreateSection(
            "Inicialização",
            "Escolha o que deve estar pronto quando você abrir o Rumo.",
            174,
            out var body);

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Toolbar,
            Padding = Padding.Empty
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        fields.Controls.Add(_restoreSession, 0, 0);
        fields.Controls.Add(MakeFieldLabel("Página inicial"), 0, 1);
        fields.Controls.Add(_homeUrl, 0, 2);
        body.Controls.Add(fields);

        return card;
    }

    private Control BuildDownloadsSection(Button browse)
    {
        var card = CreateSection(
            "Downloads",
            "Defina a pasta usada para salvar arquivos baixados.",
            174,
            out var body);

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Toolbar,
            Padding = Padding.Empty
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var folder = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Toolbar,
            Padding = Padding.Empty
        };
        folder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folder.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        folder.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        folder.Controls.Add(_downloadFolder, 0, 0);
        folder.Controls.Add(browse, 1, 0);

        fields.Controls.Add(MakeFieldLabel("Pasta de destino"), 0, 0);
        fields.Controls.Add(folder, 0, 1);
        fields.Controls.Add(_askWhereToSave, 0, 2);
        body.Controls.Add(fields);

        return card;
    }

    private Control BuildProfileSection()
    {
        var card = CreateSection(
            "Sobre este perfil",
            "Informações úteis para localizar seus dados e conferir o motor instalado.",
            166,
            out var body);

        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Toolbar,
            Padding = Padding.Empty
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        details.Controls.Add(MakeInfoLabel($"Versão do {Branding.Name}: {CurrentVersion.Display}"), 0, 0);
        details.Controls.Add(MakeInfoLabel($"Motor: Microsoft Edge WebView2 {WebViewVersion()}"), 0, 1);
        details.Controls.Add(MakeInfoLabel(
            $"Pasta de dados ({(AppPaths.IsPortable ? "portátil" : "usuário")}): {AppPaths.DataDirectory}",
            wrap: true), 0, 2);
        body.Controls.Add(details);

        return card;
    }

    private RoundedPanel CreateSection(string title, string description, int height, out Panel body)
    {
        var card = new RoundedPanel(14)
        {
            Width = 700,
            Height = height,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(14, 10, 14, 10),
            BackColor = Theme.Toolbar,
            BorderColor = Theme.AddressBorder,
            BorderWidth = 1F
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Toolbar,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            Font = new Font(Theme.Ui(11F), FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = description,
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Toolbar };
        layout.Controls.Add(body, 0, 2);
        card.Controls.Add(layout);

        return card;
    }

    private Label MakeFieldLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        ForeColor = Theme.Text,
        Font = Theme.Ui(9.5F),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private Label MakeInfoLabel(string text, bool wrap = false) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        ForeColor = Theme.Text,
        Font = Theme.Ui(9F),
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = !wrap,
        AutoSize = false,
        AccessibleDescription = text
    };

    private Control BuildFooter()
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(12, 11, 16, 8),
            BackColor = Theme.Toolbar
        };

        var save = MakeButton("Salvar configurações", 164, primary: true);
        save.DialogResult = DialogResult.OK;
        save.AccessibleName = "Salvar configurações";
        var cancel = MakeButton("Cancelar", 104);
        cancel.DialogResult = DialogResult.Cancel;

        footer.Controls.Add(save);
        footer.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;

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
            Margin = new Padding(0, 0, 8, 0),
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

    private void AdjustSectionWidths()
    {
        var width = Math.Max(620, _contentViewport.ClientSize.Width - 50);
        _sections.Width = width;
        foreach (Control section in _sections.Controls)
            section.Width = Math.Max(600, width - 28);
    }

    private void BrowseForFolder()
    {
        using var picker = new FolderBrowserDialog
        {
            Description = "Escolha a pasta de downloads",
            UseDescriptionForTitle = true,
            SelectedPath = _downloadFolder.Text
        };

        if (picker.ShowDialog(this) == DialogResult.OK)
            _downloadFolder.Text = picker.SelectedPath;
    }

    private static string WebViewVersion()
    {
        try
        {
            return Microsoft.Web.WebView2.Core.CoreWebView2Environment
                .GetAvailableBrowserVersionString() is { Length: > 0 } version
                ? version
                : "não instalado";
        }
        catch (Exception exception) when (
            exception is Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException or InvalidOperationException)
        {
            return "não instalado";
        }
    }
}
