using Microsoft.Web.WebView2.Core;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

/// <summary>
/// Gerenciamento das extensões do perfil.
///
/// O WebView2 aceita extensões descompactadas e permite ativar, desativar e
/// remover, mas não oferece a loja nem a janela de popup do ícone na barra.
/// Enquanto isso não for implementado, esta tela deixa o limite explícito em
/// vez de sugerir compatibilidade total com o Chrome.
/// </summary>
internal sealed class ExtensionsForm : Form
{
    private readonly CoreWebView2Profile _profile;
    private readonly ListBox _extensions = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Theme.Address,
        ForeColor = Theme.Text,
        BorderStyle = BorderStyle.FixedSingle,
        IntegralHeight = false,
        Font = Theme.Ui(10F)
    };
    private readonly Button _toggleButton;
    private readonly Button _removeButton;
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 24, ForeColor = Theme.MutedText };

    public ExtensionsForm(CoreWebView2Profile profile)
    {
        _profile = profile;

        Text = "Extensões do Navegador";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(620, 400);
        Size = new Size(720, 480);
        BackColor = Theme.TitleBar;
        ForeColor = Theme.Text;
        Font = Theme.Ui(10F);

        var installButton = MakeButton("Instalar pasta descompactada", 200);
        installButton.Click += async (_, _) => await InstallAsync();

        _toggleButton = MakeButton("Ativar", 110);
        _toggleButton.Enabled = false;
        _toggleButton.Click += async (_, _) => await ToggleAsync();

        _removeButton = MakeButton("Remover", 96);
        _removeButton.Enabled = false;
        _removeButton.Click += async (_, _) => await RemoveAsync();

        var reloadButton = MakeButton("Atualizar lista", 120);
        reloadButton.Click += async (_, _) => await RefreshExtensionsAsync();

        _extensions.SelectedIndexChanged += (_, _) =>
        {
            var selected = _extensions.SelectedItem is ExtensionEntry;
            _toggleButton.Enabled = selected;
            _removeButton.Enabled = selected;
            UpdateToggleCaption();
        };

        var explanation = new Label
        {
            Dock = DockStyle.Top,
            Height = 58,
            ForeColor = Theme.MutedText,
            Text =
                "Extensões Chromium podem ser carregadas de uma pasta local com manifest.json.\n" +
                "A Chrome Web Store e as janelas de popup/ícones de extensão não são suportadas pelo WebView2.",
            Padding = new Padding(12, 8, 12, 0)
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8, 8, 8, 8),
            BackColor = Theme.Toolbar,
            WrapContents = false
        };
        buttons.Controls.Add(installButton);
        buttons.Controls.Add(_toggleButton);
        buttons.Controls.Add(_removeButton);
        buttons.Controls.Add(reloadButton);

        Controls.Add(_extensions);
        Controls.Add(buttons);
        Controls.Add(_status);
        Controls.Add(explanation);

        Shown += async (_, _) => await RefreshExtensionsAsync();
    }

    private Button MakeButton(string text, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Address,
            ForeColor = Theme.Text,
            Font = Theme.Ui(9.5F),
            Margin = new Padding(0, 0, 6, 0),
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderColor = Theme.Hover;
        return button;
    }

    private async Task RefreshExtensionsAsync()
    {
        try
        {
            var installed = await _profile.GetBrowserExtensionsAsync();

            _extensions.BeginUpdate();
            _extensions.Items.Clear();

            foreach (var extension in installed)
            {
                _extensions.Items.Add(new ExtensionEntry(extension));
            }

            _extensions.EndUpdate();

            _status.Text = installed.Count switch
            {
                0 => "Nenhuma extensão instalada neste perfil.",
                1 => "1 extensão instalada.",
                _ => $"{installed.Count} extensões instaladas."
            };
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
            ShowError(
                "A pasta não contém uma extensão compatível ou o WebView2 não conseguiu carregá-la.",
                exception);
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

        var answer = MessageBox.Show(
            this,
            $"Remover {entry.Extension.Name} deste perfil?",
            "Extensões",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

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
        {
            _toggleButton.Text = entry.Extension.IsEnabled ? "Desativar" : "Ativar";
        }
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
            $"{Extension.Name} — {(Extension.IsEnabled ? "ativada" : "desativada")}  ({Extension.Id})";
    }
}
