using Microsoft.Web.WebView2.Core;

namespace Navegador.Windows;

internal sealed class ExtensionsForm : Form
{
    private static readonly Color BackgroundColor = Color.FromArgb(32, 33, 36);
    private static readonly Color ForegroundColor = Color.FromArgb(232, 234, 237);

    private readonly CoreWebView2Profile _profile;
    private readonly ListBox _extensions = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(48, 49, 52),
        ForeColor = ForegroundColor,
        BorderStyle = BorderStyle.FixedSingle,
        IntegralHeight = false
    };
    private readonly Button _toggleButton = new() { Text = "Ativar/desativar", Width = 132, Enabled = false };
    private readonly Button _removeButton = new() { Text = "Remover", Width = 92, Enabled = false };

    public ExtensionsForm(CoreWebView2Profile profile)
    {
        _profile = profile;
        Text = "Extensões do Navegador";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(560, 360);
        Size = new Size(680, 440);
        BackColor = BackgroundColor;
        ForeColor = ForegroundColor;

        var installButton = new Button { Text = "Instalar pasta descompactada", AutoSize = true };
        installButton.Click += async (_, _) => await InstallAsync();
        _toggleButton.Click += async (_, _) => await ToggleAsync();
        _removeButton.Click += async (_, _) => await RemoveAsync();
        _extensions.SelectedIndexChanged += (_, _) =>
        {
            _toggleButton.Enabled = _extensions.SelectedItem is ExtensionEntry;
            _removeButton.Enabled = _extensions.SelectedItem is ExtensionEntry;
            UpdateToggleCaption();
        };

        var explanation = new Label
        {
            Dock = DockStyle.Top,
            Height = 56,
            Text = "Extensões Chromium podem ser carregadas por uma pasta local com manifest.json.\nA instalação direta pela Chrome Web Store ainda não está disponível.",
            ForeColor = ForegroundColor,
            Padding = new Padding(10),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(6),
            BackColor = BackgroundColor
        };
        buttons.Controls.Add(installButton);
        buttons.Controls.Add(_toggleButton);
        buttons.Controls.Add(_removeButton);

        Controls.Add(_extensions);
        Controls.Add(buttons);
        Controls.Add(explanation);
        Shown += async (_, _) => await RefreshExtensionsAsync();
    }

    private async Task RefreshExtensionsAsync()
    {
        try
        {
            var installed = await _profile.GetBrowserExtensionsAsync();
            _extensions.BeginUpdate();
            _extensions.Items.Clear();
            foreach (var extension in installed)
                _extensions.Items.Add(new ExtensionEntry(extension));
            _extensions.EndUpdate();
        }
        catch (Exception exception)
        {
            ShowError("Não foi possível listar as extensões.", exception);
        }
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
            MessageBox.Show(this, $"Extensão instalada: {extension.Name}", "Extensões",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshExtensionsAsync();
        }
        catch (Exception exception)
        {
            ShowError("A pasta não contém uma extensão compatível ou o WebView2 não conseguiu carregá-la.", exception);
        }
    }

    private async Task ToggleAsync()
    {
        if (_extensions.SelectedItem is not ExtensionEntry entry) return;
        try
        {
            await entry.Extension.EnableAsync(!entry.Extension.IsEnabled);
            await RefreshExtensionsAsync();
        }
        catch (Exception exception)
        {
            ShowError("Não foi possível alterar o estado da extensão.", exception);
        }
    }

    private async Task RemoveAsync()
    {
        if (_extensions.SelectedItem is not ExtensionEntry entry) return;
        var answer = MessageBox.Show(this, $"Remover {entry.Extension.Name} deste perfil?",
            "Extensões", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        try
        {
            await entry.Extension.RemoveAsync();
            await RefreshExtensionsAsync();
        }
        catch (Exception exception)
        {
            ShowError("Não foi possível remover a extensão.", exception);
        }
    }

    private void UpdateToggleCaption()
    {
        if (_extensions.SelectedItem is ExtensionEntry entry)
            _toggleButton.Text = entry.Extension.IsEnabled ? "Desativar" : "Ativar";
    }

    private void ShowError(string message, Exception exception)
    {
        MessageBox.Show(this, $"{message}\n\n{exception.Message}", "Extensões",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private sealed class ExtensionEntry(CoreWebView2BrowserExtension extension)
    {
        public CoreWebView2BrowserExtension Extension { get; } = extension;

        public override string ToString() =>
            $"{Extension.Name} — {(Extension.IsEnabled ? "Ativada" : "Desativada")} ({Extension.Id})";
    }
}
