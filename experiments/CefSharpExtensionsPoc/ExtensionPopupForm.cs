using CefSharp.WinForms;

namespace Rumo.CefSharpPoc;

internal sealed class ExtensionPopupForm : Form
{
    public ExtensionPopupForm(string extensionName, string popupAddress)
    {
        Text = $"{extensionName} · Popup da extensão";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(420, 600);
        MinimumSize = new Size(320, 240);
        BackColor = Color.FromArgb(24, 27, 34);
        Font = new Font("Segoe UI", 9.5F);

        var browser = new ChromiumWebBrowser(popupAddress)
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(browser);
    }
}
