using System.Text.Json;
using CefSharp.WinForms;

namespace Rumo.CefSharpPoc;

internal sealed class MainForm : Form
{
    private const string StoreAddress = "https://chromewebstore.google.com/category/extensions";
    private readonly string _profileRoot;
    private readonly TextBox _address;
    private readonly TextBox _extensionId;
    private readonly ChromiumWebBrowser _browser;

    public MainForm(string profileRoot)
    {
        _profileRoot = Path.GetFullPath(profileRoot);
        Text = "Rumo · Prova de conceito CefSharp";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 620);
        Size = new Size(1260, 820);
        BackColor = Color.FromArgb(24, 27, 34);
        ForeColor = Color.FromArgb(235, 238, 245);
        Font = new Font("Segoe UI", 9.5F);

        _address = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = StoreAddress,
            Font = new Font("Segoe UI", 10F),
            Margin = new Padding(8, 5, 8, 5)
        };

        _extensionId = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            Margin = new Padding(8, 5, 8, 5),
            MaxLength = 32,
            AccessibleName = "ID da extensão instalada"
        };

        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = BackColor,
            Padding = new Padding(8, 4, 8, 4),
            Margin = Padding.Empty
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78F));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 164F));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var popupToolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = BackColor,
            Padding = new Padding(8, 4, 8, 4),
            Margin = Padding.Empty
        };
        popupToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F));
        popupToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        popupToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        popupToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _browser = new ChromiumWebBrowser(StoreAddress)
        {
            Dock = DockStyle.Fill
        };

        var go = MakeButton("Ir");
        go.Click += (_, _) => NavigateAddress();
        var extensions = MakeButton("Extensões");
        extensions.Click += (_, _) => _browser.Load("chrome://extensions/");

        var extensionIdLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "ID da extensão",
            ForeColor = ForeColor,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0)
        };
        var openPopup = MakeButton("Abrir popup");
        openPopup.Click += (_, _) => OpenExtensionPopup();

        toolbar.Controls.Add(_address, 0, 0);
        toolbar.Controls.Add(go, 1, 0);
        toolbar.Controls.Add(extensions, 2, 0);
        popupToolbar.Controls.Add(extensionIdLabel, 0, 0);
        popupToolbar.Controls.Add(_extensionId, 1, 0);
        popupToolbar.Controls.Add(openPopup, 2, 0);

        var profileNote = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Perfil de teste persistente: {_profileRoot}",
            ForeColor = Color.FromArgb(176, 184, 198),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0),
            AccessibleName = "Local do perfil persistente de teste"
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = BackColor,
            Padding = new Padding(10),
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(popupToolbar, 0, 1);
        layout.Controls.Add(_browser, 0, 2);
        layout.Controls.Add(profileNote, 0, 3);

        Controls.Add(layout);
        _address.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode != Keys.Enter) return;
            eventArgs.SuppressKeyPress = true;
            NavigateAddress();
        };
        _extensionId.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode != Keys.Enter) return;
            eventArgs.SuppressKeyPress = true;
            OpenExtensionPopup();
        };
    }

    private void NavigateAddress()
    {
        var address = _address.Text.Trim();
        if (address.Length == 0) return;

        if (!address.Contains("://", StringComparison.Ordinal))
        {
            address = $"https://{address}";
        }

        _browser.Load(address);
    }

    private void OpenExtensionPopup()
    {
        var extensionId = _extensionId.Text.Trim().ToLowerInvariant();
        if (extensionId.Length != 32 || extensionId.Any(character => character is < 'a' or > 'p'))
        {
            ShowPopupError("Informe o ID de 32 caracteres exibido em chrome://extensions.");
            return;
        }

        var extensionDirectory = Path.Combine(_profileRoot, "Default", "Extensions", extensionId);
        if (!Directory.Exists(extensionDirectory))
        {
            ShowPopupError("Não encontrei essa extensão no perfil da PoC. Instale-a e confira o ID em chrome://extensions.");
            return;
        }

        string[] manifests;
        try
        {
            manifests = Directory
                .EnumerateFiles(extensionDirectory, "manifest.json", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowPopupError($"Não consegui ler os arquivos da extensão: {exception.Message}");
            return;
        }

        foreach (var manifestPath in manifests)
        {
            try
            {
                using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
                var root = manifest.RootElement;
                var popupPath = ReadPopupPath(root, "action") ?? ReadPopupPath(root, "browser_action");
                if (string.IsNullOrWhiteSpace(popupPath)) continue;

                var extensionName = root.TryGetProperty("name", out var nameElement)
                    && nameElement.ValueKind == JsonValueKind.String
                    ? nameElement.GetString() ?? "Extensão"
                    : "Extensão";
                var extensionBase = new Uri($"chrome-extension://{extensionId}/");
                var popupUri = new Uri(extensionBase, popupPath.Replace('\\', '/'));
                if (popupUri.Scheme != "chrome-extension"
                    || !popupUri.Host.Equals(extensionId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                new ExtensionPopupForm(extensionName, popupUri.AbsoluteUri).Show(this);
                return;
            }
            catch (JsonException)
            {
                // Ignora um manifesto incompleto de outra versão e tenta o seguinte.
            }
            catch (UriFormatException)
            {
                // Um caminho inválido no manifesto não deve abrir uma URL externa.
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                ShowPopupError($"Não consegui ler o manifesto da extensão: {exception.Message}");
                return;
            }
        }

        ShowPopupError(manifests.Length == 0
            ? "Não encontrei o manifesto instalado dessa extensão."
            : "O manifesto não declara um popup em action.default_popup ou browser_action.default_popup.");
    }

    private static string? ReadPopupPath(JsonElement manifest, string actionKey)
    {
        if (!manifest.TryGetProperty(actionKey, out var action)
            || action.ValueKind != JsonValueKind.Object
            || !action.TryGetProperty("default_popup", out var popup)
            || popup.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return popup.GetString();
    }

    private void ShowPopupError(string message) => MessageBox.Show(
        this,
        message,
        "Rumo · PoC CefSharp",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);

    private static Button MakeButton(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(48, 54, 66),
        ForeColor = Color.FromArgb(235, 238, 245),
        Margin = new Padding(4),
        Cursor = Cursors.Hand
    };
}
