using CefSharp.WinForms;

namespace Rumo.CefSharpPoc;

internal sealed class MainForm : Form
{
    private const string StoreAddress = "https://chromewebstore.google.com/category/extensions";
    private readonly TextBox _address;
    private readonly ChromiumWebBrowser _browser;

    public MainForm(string profileRoot)
    {
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

        _browser = new ChromiumWebBrowser(StoreAddress)
        {
            Dock = DockStyle.Fill
        };

        var go = MakeButton("Ir");
        go.Click += (_, _) => NavigateAddress();
        var extensions = MakeButton("Extensões");
        extensions.Click += (_, _) => _browser.Load("chrome://extensions/");

        toolbar.Controls.Add(_address, 0, 0);
        toolbar.Controls.Add(go, 1, 0);
        toolbar.Controls.Add(extensions, 2, 0);

        var profileNote = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Perfil de teste persistente: {profileRoot}",
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
            RowCount = 3,
            BackColor = BackColor,
            Padding = new Padding(10),
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(_browser, 0, 1);
        layout.Controls.Add(profileNote, 0, 2);

        Controls.Add(layout);
        _address.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode != Keys.Enter) return;
            eventArgs.SuppressKeyPress = true;
            NavigateAddress();
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
