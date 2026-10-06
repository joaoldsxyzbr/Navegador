using System.Runtime.InteropServices;

namespace Navegador.Windows;

internal sealed partial class BrowserForm
{
    private const int ResizeBorder = 6;
    private const int TitleRowHeight = 42;

    private const int WmNcHitTest = 0x0084;
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmSetCursor = 0x0020;
    private const int WmNcLButtonDblClk = 0x00A3;

    private const int HtClient = 1;
    private const int HtCaption = 2;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;

    private static readonly IntPtr CursorArrow = LoadCursor(IntPtr.Zero, 32512); // IDC_ARROW
    private static readonly IntPtr CursorSizeNwse = LoadCursor(IntPtr.Zero, 32642); // IDC_SIZENWSE
    private static readonly IntPtr CursorSizeNesw = LoadCursor(IntPtr.Zero, 32643); // IDC_SIZENESW
    private static readonly IntPtr CursorSizeWe = LoadCursor(IntPtr.Zero, 32644); // IDC_SIZEWE
    private static readonly IntPtr CursorSizeNs = LoadCursor(IntPtr.Zero, 32645); // IDC_SIZENS

    private int _lastHitTest = HtClient;

    private void ToggleMaximize()
    {
        if (WindowState == FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Normal;
            return;
        }

        UpdateMaximizedBounds();
        WindowState = FormWindowState.Maximized;
    }

    private void UpdateMaximizedBounds()
    {
        if (!IsHandleCreated) return;

        // Sem isto a janela maximizada cobre a barra de tarefas.
        MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
    }

    private void BeginWindowDrag(object? sender, MouseEventArgs eventArgs)
    {
        if (eventArgs.Button != MouseButtons.Left) return;

        if (WindowState == FormWindowState.Maximized) WindowState = FormWindowState.Normal;

        ReleaseCapture();
        SendMessage(Handle, WmNcLButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmNcHitTest && WindowState == FormWindowState.Normal)
        {
            base.WndProc(ref message);

            if ((int)message.Result != HtClient)
            {
                _lastHitTest = (int)message.Result;
                return;
            }

            var packed = message.LParam.ToInt64();
            var screenPoint = new Point(
                unchecked((short)(packed & 0xFFFF)),
                unchecked((short)((packed >> 16) & 0xFFFF)));
            var clientPoint = PointToClient(screenPoint);

            var left = clientPoint.X <= ResizeBorder;
            var right = clientPoint.X >= ClientSize.Width - ResizeBorder;
            var top = clientPoint.Y <= ResizeBorder;
            var bottom = clientPoint.Y >= ClientSize.Height - ResizeBorder;

            var result = HtClient;

            if (left && top) result = HtTopLeft;
            else if (right && top) result = HtTopRight;
            else if (left && bottom) result = HtBottomLeft;
            else if (right && bottom) result = HtBottomRight;
            else if (left) result = HtLeft;
            else if (right) result = HtRight;
            else if (top) result = HtTop;
            else if (bottom) result = HtBottom;

            _lastHitTest = result;
            message.Result = (IntPtr)result;
            return;
        }

        if (message.Msg == WmSetCursor)
        {
            // Sem isto o cursor de redimensionamento nunca aparece, porque a
            // janela não tem moldura padrão do Windows.
            SetCursor(CursorForHitTest(_lastHitTest));
            message.Result = (IntPtr)1;
            return;
        }

        base.WndProc(ref message);
    }

    private static IntPtr CursorForHitTest(int hitTest) => hitTest switch
    {
        HtLeft or HtRight => CursorSizeWe,
        HtTop or HtBottom => CursorSizeNs,
        HtTopLeft or HtBottomRight => CursorSizeNwse,
        HtTopRight or HtBottomLeft => CursorSizeNesw,
        _ => CursorArrow
    };

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr instance, int cursorId);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCursor(IntPtr cursor);
}
