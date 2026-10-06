using System.Drawing.Drawing2D;

namespace Navegador.Windows.Ui;

internal static class Theme
{
    public static readonly Color TitleBar = Color.FromArgb(32, 33, 36);
    public static readonly Color Toolbar = Color.FromArgb(41, 42, 45);
    public static readonly Color ActiveTab = Color.FromArgb(53, 54, 58);
    public static readonly Color Address = Color.FromArgb(32, 33, 36);
    public static readonly Color AddressFocus = Color.FromArgb(39, 40, 43);
    public static readonly Color AddressBorder = Color.FromArgb(74, 77, 81);
    public static readonly Color Hover = Color.FromArgb(60, 64, 67);
    public static readonly Color Press = Color.FromArgb(74, 76, 80);
    public static readonly Color Text = Color.FromArgb(232, 234, 237);
    public static readonly Color MutedText = Color.FromArgb(154, 160, 166);
    public static readonly Color CloseHover = Color.FromArgb(196, 43, 28);
    public static readonly Color Accent = Color.FromArgb(138, 180, 248);
    public static readonly Color PageBackground = Color.FromArgb(32, 33, 36);
    public static readonly Color Starred = Color.FromArgb(251, 188, 4);

    public static Font Ui(float size = 10F) => new("Segoe UI", size);
    public static Font Icon(float size = 11.5F) => new("Segoe UI", size);
}

internal enum BrowserIcon { None, Add, Back, Forward, Reload, Star, StarFilled, Download, Update, Extensions, Menu }

internal sealed class RoundedPanel : Panel
{
    private readonly int _radius;
    private Color _borderColor = Color.Transparent;
    private float _borderWidth = 1F;

    public RoundedPanel(int radius)
    {
        _radius = radius;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    internal Color BorderColor
    {
        get => _borderColor;
        set { if (_borderColor != value) { _borderColor = value; Invalidate(); } }
    }

    internal float BorderWidth
    {
        get => _borderWidth;
        set { _borderWidth = Math.Max(0F, value); Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = ClientRectangle;
        bounds.Width = Math.Max(1, bounds.Width - 1);
        bounds.Height = Math.Max(1, bounds.Height - 1);
        using var path = RoundedGeometry.CreatePath(bounds, _radius);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);

        if (_borderColor != Color.Transparent && _borderWidth > 0)
        {
            using var pen = new Pen(_borderColor, _borderWidth);
            e.Graphics.DrawPath(pen, path);
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e) =>
        e.Graphics.Clear(Parent?.BackColor ?? Theme.Toolbar);
}

internal class IconButton : Button
{
    private bool _hovered;
    private bool _pressed;

    internal BrowserIcon Icon { get; set; }
    internal Color HoverColor { get; set; } = Theme.Hover;
    internal Color PressColor { get; set; } = Theme.Press;

    public IconButton()
    {
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        TabStop = false;
        FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var parentBrush = new SolidBrush(Parent?.BackColor ?? Theme.Toolbar);
        g.FillRectangle(parentBrush, ClientRectangle);

        var diameter = Math.Max(1, Math.Min(Width, Height) - 4);
        var circle = new RectangleF((Width - diameter) / 2F, (Height - diameter) / 2F, diameter, diameter);
        var fillColor = !Enabled ? BackColor : _pressed ? PressColor : _hovered ? HoverColor : BackColor;
        using (var fill = new SolidBrush(fillColor)) g.FillEllipse(fill, circle);

        var bounds = RectangleF.Inflate(circle, -8F, -8F);
        var color = Enabled ? ForeColor : Theme.MutedText;

        if (Icon == BrowserIcon.None)
        {
            TextRenderer.DrawText(g, Text, Font, Rectangle.Round(circle), color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            return;
        }

        DrawIcon(g, Icon, bounds, color);
    }

    private static void DrawIcon(Graphics g, BrowserIcon icon, RectangleF b, Color color)
    {
        using var pen = new Pen(color, 1.8F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        var cx = b.Left + b.Width / 2F; var cy = b.Top + b.Height / 2F;
        var l = b.Left; var rr = b.Right; var t = b.Top; var bot = b.Bottom;

        switch (icon)
        {
            case BrowserIcon.Add:
                g.DrawLine(pen, cx, t + 2, cx, bot - 2); g.DrawLine(pen, l + 2, cy, rr - 2, cy); break;
            case BrowserIcon.Back:
                g.DrawLine(pen, rr - 2, cy, l + 3, cy); g.DrawLine(pen, l + 3, cy, cx - 1, t + 3); g.DrawLine(pen, l + 3, cy, cx - 1, bot - 3); break;
            case BrowserIcon.Forward:
                g.DrawLine(pen, l + 2, cy, rr - 3, cy); g.DrawLine(pen, rr - 3, cy, cx + 1, t + 3); g.DrawLine(pen, rr - 3, cy, cx + 1, bot - 3); break;
            case BrowserIcon.Reload:
                g.DrawArc(pen, RectangleF.Inflate(b, -2, -2), 35, 285); g.DrawLine(pen, rr - 2, t + 4, rr - 2, t + 9); g.DrawLine(pen, rr - 2, t + 4, rr - 7, t + 4); break;
            case BrowserIcon.Star:
            case BrowserIcon.StarFilled:
                using (var star = CreateStar(b))
                {
                    if (icon == BrowserIcon.StarFilled) { using var brush = new SolidBrush(color); g.FillPath(brush, star); }
                    else g.DrawPath(pen, star);
                }
                break;
            case BrowserIcon.Download:
                g.DrawLine(pen, cx, t + 1, cx, cy + 4); g.DrawLine(pen, cx, cy + 4, cx - 5, cy - 1); g.DrawLine(pen, cx, cy + 4, cx + 5, cy - 1); g.DrawLine(pen, l + 2, bot - 2, rr - 2, bot - 2); break;
            case BrowserIcon.Update:
                g.DrawArc(pen, RectangleF.Inflate(b, -2, -2), 205, 260); g.DrawLine(pen, rr - 1, t + 5, rr - 1, t + 10); g.DrawLine(pen, rr - 1, t + 5, rr - 6, t + 5);
                g.DrawLine(pen, cx, cy - 4, cx, bot - 3); g.DrawLine(pen, cx, bot - 3, cx - 4, bot - 7); g.DrawLine(pen, cx, bot - 3, cx + 4, bot - 7); break;
            case BrowserIcon.Extensions:
                var w = b.Width / 3.4F; var h = b.Height / 3.4F;
                using (var brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, cx - w / 2, t + 1, w, h); g.FillEllipse(brush, l + 1, bot - h - 1, w, h); g.FillEllipse(brush, rr - w - 1, bot - h - 1, w, h);
                }
                g.DrawLine(pen, cx, t + h, cx, cy); g.DrawLine(pen, cx, cy, l + w / 2 + 1, bot - h); g.DrawLine(pen, cx, cy, rr - w / 2 - 1, bot - h); break;
            case BrowserIcon.Menu:
                using (var brush = new SolidBrush(color))
                {
                    const float d = 2.6F;
                    g.FillEllipse(brush, cx - d / 2, t + 2, d, d); g.FillEllipse(brush, cx - d / 2, cy - d / 2, d, d); g.FillEllipse(brush, cx - d / 2, bot - d - 2, d, d);
                }
                break;
        }
    }

    private static GraphicsPath CreateStar(RectangleF b)
    {
        var path = new GraphicsPath();
        var center = new PointF(b.Left + b.Width / 2F, b.Top + b.Height / 2F);
        var outer = Math.Min(b.Width, b.Height) / 2F; var inner = outer * .46F;
        var points = new PointF[10];

        for (var i = 0; i < points.Length; i++)
        {
            var radius = i % 2 == 0 ? outer : inner;
            var angle = -MathF.PI / 2F + i * MathF.PI / 5F;
            points[i] = new PointF(center.X + MathF.Cos(angle) * radius, center.Y + MathF.Sin(angle) * radius);
        }

        path.AddPolygon(points); path.CloseFigure(); return path;
    }
}

internal sealed class WindowButton : Button
{
    public WindowButton(bool close)
    {
        FlatStyle = FlatStyle.Flat; UseVisualStyleBackColor = false; TextAlign = ContentAlignment.MiddleCenter;
        TabStop = false; Margin = Padding.Empty; BackColor = Theme.TitleBar; ForeColor = Theme.Text; FlatAppearance.BorderSize = 0;
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
        if (diameter <= 1) { path.AddRectangle(bounds); return path; }

        var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
        path.AddArc(arc, 180, 90); arc.X = bounds.Right - diameter; path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter; path.AddArc(arc, 0, 90); arc.X = bounds.Left; path.AddArc(arc, 90, 90);
        path.CloseFigure(); return path;
    }
}
