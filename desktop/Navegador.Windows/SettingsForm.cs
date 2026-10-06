using Navegador.Core;
using Navegador.Core.Storage;
using Navegador.Core.Updates;
using Navegador.Windows.Ui;
using CefSharp;

namespace Navegador.Windows;

/// <summary>Preferências do usuário e informações da instalação.</summary>
internal sealed class SettingsForm : Form
{
    private readonly SettingsStore _settings;
    private readonly TextBox _downloadFolder = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _homeUrl = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly CheckBox _restoreSession = new() { Text = "Reabrir as abas da última sessão ao iniciar", AutoSize = true };
    private readonly CheckBox _askWhereToSave = new() { Text = "Perguntar onde salvar antes de cada download", AutoSize = true };

    public SettingsForm(SettingsStore settings)
    {
        _settings = settings;

        Text = $"Configurações do {Branding.Name}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(560, 380);
        BackColor = Theme.TitleBar;
        ForeColor = Theme.Text;
        Font = Theme.Ui(10F);
        MaximizeBox = false;
        MinimizeBox = false;

        foreach (var box in new[] { _downloadFolder, _homeUrl })
        {
            box.BackColor = Theme.Address;
            box.ForeColor = Theme.Text;
        }

        foreach (var check in new[] { _restoreSession, _askWhereToSave })
        {
            check.ForeColor = Theme.Text;
            check.BackColor = Theme.TitleBar;
            check.FlatStyle = FlatStyle.Flat;
        }

        _restoreSession.Checked = settings.Current.RestoreSession;
        _askWhereToSave.Checked = settings.Current.AskWhereToSaveDownloads;
        _downloadFolder.Text = settings.Current.DownloadFolder;
        _homeUrl.Text = settings.Current.HomeUrl;

        var browse = new Button
        {
            Text = "Escolher…",
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Address,
            ForeColor = Theme.Text,
            Width = 110,
            Dock = DockStyle.Fill
        };
        browse.FlatAppearance.BorderColor = Theme.Hover;
        browse.Click += (_, _) => BrowseForFolder();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(14),
            BackColor = Theme.TitleBar
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));

        AddRow(layout, "Pasta de downloads", _downloadFolder, browse);
        AddRow(layout, "Página inicial", _homeUrl, null);
        AddRow(layout, string.Empty, _restoreSession, null);
        AddRow(layout, string.Empty, _askWhereToSave, null);

        var info = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Theme.MutedText,
            TextAlign = ContentAlignment.TopLeft,
            Text =
                $"{Branding.Name} {CurrentVersion.Display}\n" +
                $"Dados: {AppPaths.DataDirectory}\n" +
                $"Modo: {(AppPaths.IsPortable ? "portátil (ao lado do executável)" : "perfil do usuário")}\n" +
                $"Chromium: {Cef.ChromiumVersion}"
        };

        layout.Controls.Add(info, 0, layout.RowCount);
        layout.SetColumnSpan(info, 2);

        var save = new Button
        {
            Text = "Salvar",
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.AddressFocus,
            ForeColor = Theme.Text,
            Width = 100
        };
        var cancel = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Address,
            ForeColor = Theme.Text,
            Width = 100
        };
        save.FlatAppearance.BorderColor = Theme.Hover;
        cancel.FlatAppearance.BorderColor = Theme.Hover;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 48,
            Padding = new Padding(8, 8, 8, 8),
            BackColor = Theme.Toolbar
        };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);

        Controls.Add(layout);
        Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
    }

    /// <summary>Aplica o que o usuário escolheu. Devolve <c>true</c> quando algo mudou.</summary>
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

    private static void AddRow(TableLayoutPanel layout, string label, Control field, Control? trailing)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        layout.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Text
        }, 0, row);

        if (field is CheckBox check)
        {
            layout.Controls.Add(check, 0, row);
            layout.SetColumnSpan(check, 2);
            return;
        }

        var holder = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = trailing is null ? 1 : 2,
            RowCount = 1,
            Padding = new Padding(0, 8, 8, 8)
        };
        holder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        if (trailing is not null) holder.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114));

        holder.Controls.Add(field, 0, 0);
        if (trailing is not null) holder.Controls.Add(trailing, 1, 0);

        layout.Controls.Add(holder, 0, row);
        layout.SetColumnSpan(holder, 2);
    }

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

}
