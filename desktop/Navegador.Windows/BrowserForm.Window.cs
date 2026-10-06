using System.Runtime.InteropServices;

namespace Navegador.Windows;

internal sealed partial class BrowserForm
{
    private const int ResizeBorder = 6;
    private const int TitleRowHeight = 42;

    private const int WmGetMinMaxInfo = 0x0024;
    private const int WmNcHitTest = 0x0084;
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmSetCursor = 0x0020;
    private const int WmNcLButtonDblClk = 0x00A3;

    private const uint MonitorDefaultToNearest = 0x00000002;

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
    private bool _isFullscreen;
    private FormWindowState _windowStateBeforeFullscreen;
    private Rectangle _boundsBeforeFullscreen;
    private float _titleRowBeforeFullscreen;
    private float _toolbarRowBeforeFullscreen;
    private bool _downloadsVisibleBeforeFullscreen;

    private void ToggleMaximize()
    {
        if (_isFullscreen) return;
        WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    private void ToggleFullscreen()
    {
        if (_rootLayout is null) return;

        if (!_isFullscreen)
        {
            _windowStateBeforeFullscreen = WindowState;
            _boundsBeforeFullscreen = WindowState == FormWindowState.Maximized ? RestoreBounds : Bounds;
            _titleRowBeforeFullscreen = _rootLayout.RowStyles[0].Height;
            _toolbarRowBeforeFullscreen = _rootLayout.RowStyles[1].Height;
            _downloadsVisibleBeforeFullscreen = _downloadsBar.Visible;

            _isFullscreen = true;
            WindowState = FormWindowState.Normal;
            _rootLayout.SuspendLayout();
            _rootLayout.RowStyles[0].Height = 0;
            _rootLayout.RowStyles[1].Height = 0;
            _favoritesRowStyle.Height = 0;
            _rootLayout.RowStyles[4].Height = 0;
            _favoritesBar.Visible = false;
            _downloadsBar.Visible = false;
            _rootLayout.ResumeLayout(performLayout: true);
            Padding = Padding.Empty;
            Bounds = Screen.FromControl(this).Bounds;
            return;
        }

        _isFullscreen = false;
        WindowState = FormWindowState.Normal;
        Bounds = _boundsBeforeFullscreen;
        _rootLayout.SuspendLayout();
        _rootLayout.RowStyles[0].Height = _titleRowBeforeFullscreen;
        _rootLayout.RowStyles[1].Height = _toolbarRowBeforeFullscreen;
        _favoritesRowStyle.Height = _favorites.Count > 0 ? 32 : 0;
        _rootLayout.RowStyles[4].Height = _downloadsVisibleBeforeFullscreen ? _downloadsBar.Height : 0;
        _favoritesBar.Visible = _favorites.Count > 0;
        _downloadsBar.Visible = _downloadsVisibleBeforeFullscreen;
        _rootLayout.ResumeLayout(performLayout: true);
        WindowState = _windowStateBeforeFullscreen;
        Padding = WindowState == FormWindowState.Maximized ? Padding.Empty : new Padding(1);
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
        if (message.Msg == WmGetMinMaxInfo && TrySetMaximizedWorkingArea(message.LParam))
        {
            message.Result = IntPtr.Zero;
            return;
        }

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

    private bool TrySetMaximizedWorkingArea(IntPtr parameter)
    {
        if (parameter == IntPtr.Zero) return false;

        var monitor = MonitorFromWindow(Handle, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero) return false;

        var monitorInfo = new NativeMonitorInfo { Size = Marshal.SizeOf<NativeMonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo)) return false;

        var bounds = Marshal.PtrToStructure<NativeMinMaxInfo>(parameter);
        bounds.MaxPosition = new NativePoint
        {
            X = monitorInfo.WorkArea.Left - monitorInfo.Monitor.Left,
            Y = monitorInfo.WorkArea.Top - monitorInfo.Monitor.Top
        };
        bounds.MaxSize = new NativePoint
        {
            X = monitorInfo.WorkArea.Right - monitorInfo.WorkArea.Left,
            Y = monitorInfo.WorkArea.Bottom - monitorInfo.WorkArea.Top
        };

        Marshal.StructureToPtr(bounds, parameter, false);
        return true;
    }

    private static IntPtr CursorForHitTest(int hitTest) => hitTest switch
    {
        HtLeft or HtRight => CursorSizeWe,
        HtTop or HtBottom => CursorSizeNs,
        HtTopLeft or HtBottomRight => CursorSizeNwse,
        HtTopRight or HtBottomLeft => CursorSizeNesw,
        _ => CursorArrow
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref NativeMonitorInfo monitorInfo);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr instance, int cursorId);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCursor(IntPtr cursor);
}
