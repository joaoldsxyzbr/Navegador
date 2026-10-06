using System.Drawing.Drawing2D;

namespace Navegador.Windows.Ui;

/// <summary>Cores e fontes do shell, no tom escuro do Chrome.</summary>
internal static class Theme
{
    public static readonly Color TitleBar = Color.FromArgb(32, 33, 36);
    public static readonly Color Toolbar = Color.FromArgb(41, 42, 45);
    public static readonly Color ActiveTab = Color.FromArgb(53, 54, 58);
    public static readonly Color Address = Color.FromArgb(48, 49, 52);
    public static readonly Color AddressFocus = Color.FromArgb(57, 58, 62);
    public static readonly Color Hover = Color.FromArgb(60, 64, 67);
    public static readonly Color Press = Color.FromArgb(74, 76, 80);
    public static readonly Color Text = Color.FromArgb(232, 234, 237);
    public static readonly Color MutedText = Color.FromArgb(154, 160, 166);
    public static readonly Color CloseHover = Color.FromArgb(196, 43, 28);
    public static readonly Color Accent = Color.FromArgb(138, 180, 248);
    public static readonly Color PageBackground = Color.FromArgb(32, 33, 36);

    /// <summary>Vermelho do favorito marcado.</summary>
    public static readonly Color Starred = Color.FromArgb(242, 153, 74);

    public static Font Ui(float size = 10F) => new("Segoe UI", size);

    public static Font Icon(float size = 11.5F) => new("Segoe UI", size);

    public static Font Symbol(float size = 12F) => new("Segoe UI Symbol", size);
}

/// <summary>Desenha um painel com cantos arredondados sem usar Region.</summary>
internal sealed class RoundedPanel : Panel
{
    private readonly int _radius;

    public RoundedPanel(int radius)
    {
        _radius = radius;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }

    public Color BorderColor { get; set; } = Color.Transparent;

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = RoundedGeometry.CreatePath(ClientRectangle, _radius);
        using var brush = new SolidBrush(BackColor);
        eventArgs.Graphics.FillPath(brush, path);

        if (BorderColor != Color.Transparent)
        {
            using var pen = new Pen(BorderColor);
            eventArgs.Graphics.DrawPath(pen, path);
        }
    }

    protected override void OnPaintBackground(PaintEventArgs eventArgs)
    {
        // O fundo é pintado junto com a forma arredondada em OnPaint para evitar
        // o quadrado claro aparecendo nos cantos.
    }
}

/// <summary>
/// Botão redondo da barra de ferramentas.
///
/// O estado desabilitado também muda o fundo e o realce do mouse: com
/// <see cref="FlatStyle.Flat"/> o WinForms mantém a aparência clicável, o que
/// fazia "Voltar" parecer disponível quando não havia histórico.
/// </summary>
internal class IconButton : Button
{
    private Color _hoverColor = Theme.Hover;
    private Color _pressColor = Theme.Press;

    public IconButton()
    {
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
        TextAlign = ContentAlignment.MiddleCenter;
        Cursor = Cursors.Hand;
        TabStop = false;
        FlatAppearance.BorderSize = 0;
    }

    public Color HoverColor
    {
        get => _hoverColor;
        set
        {
            _hoverColor = value;
            ApplyStateColors();
        }
    }

    public Color PressColor
    {
        get => _pressColor;
        set
        {
            _pressColor = value;
            ApplyStateColors();
        }
    }

    protected override void OnEnabledChanged(EventArgs eventArgs)
    {
        base.OnEnabledChanged(eventArgs);
        ApplyStateColors();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var diameter = Math.Min(Width, Height);
        var bounds = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);

        using var path = new GraphicsPath();
        path.AddEllipse(bounds);

        // O fundo só é pintado dentro do círculo; o resto fica com o pai.
        using var background = new SolidBrush(Parent?.BackColor ?? Theme.Toolbar);
        eventArgs.Graphics.FillRectangle(background, ClientRectangle);
        using var fill = new SolidBrush(BackColor);
        eventArgs.Graphics.FillPath(fill, path);

        TextRenderer.DrawText(
            eventArgs.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    protected override void OnPaintBackground(PaintEventArgs eventArgs)
    {
        // Tudo é pintado em OnPaint.
    }

    private void ApplyStateColors()
    {
        FlatAppearance.MouseOverBackColor = Enabled ? _hoverColor : BackColor;
        FlatAppearance.MouseDownBackColor = Enabled ? _pressColor : BackColor;
        ForeColor = Enabled ? Theme.Text : Theme.MutedText;
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }
}

/// <summary>Botão de um dos três controles da janela (minimizar, maximizar, fechar).</summary>
internal sealed class WindowButton : Button
{
    public WindowButton(bool close)
    {
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
        TextAlign = ContentAlignment.MiddleCenter;
        TabStop = false;
        Margin = Padding.Empty;
        BackColor = Theme.TitleBar;
        ForeColor = Theme.Text;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = close ? Theme.CloseHover : Theme.Hover;
        FlatAppearance.MouseDownBackColor = close ? Theme.CloseHover : Theme.Press;
    }
}

internal static class RoundedGeometry
{
    public static GraphicsPath CreatePath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));

        if (diameter <= 1)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
