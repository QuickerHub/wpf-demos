using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CeaViewRunner.Infrastructure;

public enum WindowLocations
{
    [Display(Name = "自动")]
    NA = 0,
    [Display(Name = "屏幕上左")]
    TopLeft = 1,
    [Display(Name = "屏幕上中")]
    TopCenter = 2,
    [Display(Name = "屏幕上右")]
    TopRight = 3,
    [Display(Name = "屏幕左侧")]
    LeftCenter = 4,
    [Display(Name = "屏幕中心")]
    CenterScreen = 5,
    [Display(Name = "屏幕右侧")]
    RightCenter = 6,
    [Display(Name = "屏幕左下")]
    BottomLeft = 7,
    [Display(Name = "屏幕下侧")]
    BottomCenter = 8,
    [Display(Name = "屏幕右下")]
    BottomRight = 9,
    [Display(Name = "鼠标周围")]
    MouseAround = 10,
    [Display(Name = "鼠标右下")]
    MouseRightBottom = 11,
    [Display(Name = "屏幕上")]
    InScreen = 12,
}

internal static class NativeWin32
{
    internal const int GWL_EXSTYLE = -20;
    internal const uint WS_EX_NOACTIVATE = 0x08000000;
    internal const uint WS_EX_TRANSPARENT = 0x00000020;
    internal const uint WS_EX_TOPMOST = 0x00000008;

    internal const uint SWP_DRAWFRAME = 0x0020;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtr")]
    internal static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
    internal static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    internal static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) =>
        IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong32(hWnd, nIndex));

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    internal static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
        {
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }

        return new IntPtr(SetWindowLong(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }
}

/// <summary>
/// Subset of Cea.Utils.WindowHelper used by ViewRunner.ShowWindow.
/// </summary>
public static class ViewRunnerWindowHelper
{
    public static IntPtr GetHandle(this Window window)
    {
        try
        {
            return new WindowInteropHelper(window).Handle;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    public static Window? GetWindow(IntPtr handle)
    {
        try
        {
            return HwndSource.FromHwnd(handle)?.RootVisual as Window;
        }
        catch
        {
            return null;
        }
    }

    public static void ShowWindow(Window window, bool active = false, Action? afterShow = null)
    {
        if (window == null)
        {
            return;
        }

        try
        {
            if (!window.IsVisible)
            {
                window.Show();
                afterShow?.Invoke();
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            if (active)
            {
                window.Activate();
            }
        }
        catch
        {
            // ignore
        }
    }

    public static void ShowWindowAndWaitClose(Window window, bool activate = false, Action? afterShow = null)
    {
        var frame = new DispatcherFrame();
        window.Closed += (_, _) => { frame.Continue = false; };
        window.Show();
        afterShow?.Invoke();
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        if (activate)
        {
            window.Activate();
        }

        Dispatcher.PushFrame(frame);
    }

    public static void DoActionOnLoaded(this Window window, Action action, int delayMs = 0)
    {
        void ExecuteAction()
        {
            if (delayMs == 0)
            {
                action();
            }
            else
            {
                Task.Delay(delayMs).ContinueWith(_ =>
                    System.Windows.Application.Current?.Dispatcher.Invoke(action), TaskScheduler.Default);
            }
        }

        if (window.IsLoaded)
        {
            ExecuteAction();
        }
        else
        {
            void loaded(object? s, RoutedEventArgs e)
            {
                window.Loaded -= loaded;
                ExecuteAction();
            }

            window.Loaded += loaded;
        }
    }

    public static void MoveWindow(Window win, WindowLocations location) => MoveWindow(win.GetHandle(), location);

    public static void MoveWindow(IntPtr handle, WindowLocations location)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        if (location == WindowLocations.NA)
        {
            MoveWindowInToScreen(handle);
            return;
        }

        if (location == WindowLocations.MouseAround)
        {
            MoveWindowToMouseAround(handle);
            return;
        }

        if (location == WindowLocations.MouseRightBottom)
        {
            MoveWindowToMouseBottomRight(handle);
            return;
        }

        if (location == WindowLocations.InScreen)
        {
            MoveWindowIntoCursorScreen(handle);
            return;
        }

        var num = (int)location - 1;
        var workingArea = GetWorkingAreaByCursor();
        NativeWin32.GetWindowRect(handle, out var wr);

        var horPx = (int)((workingArea.Width - wr.Width) / 2.0 * (num % 3));
        var verPx = (int)((workingArea.Height - wr.Height) / 2.0 * (num / 3));
        MoveWindow(handle, workingArea.Left + horPx, workingArea.Top + verPx);
    }

    public static void MoveWindowInToScreen(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var area = Screen.FromHandle(handle).WorkingArea;
        NativeWin32.GetWindowRect(handle, out var wr);
        var maxLeft = area.Right - Math.Min(area.Width, wr.Width);
        var maxTop = area.Bottom - Math.Min(area.Height, wr.Height);
        var x = Limit(wr.Left, area.Left, maxLeft);
        var y = Limit(wr.Top, area.Top, maxTop);
        MoveWindow(handle, x, y);
    }

    public static void MoveWindowInToScreen(IntPtr handle, int x, int y)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var area = Screen.FromPoint(new System.Drawing.Point(x, y)).WorkingArea;
        NativeWin32.GetWindowRect(handle, out var wr);
        var maxLeft = area.Right - Math.Min(area.Width, wr.Width);
        var maxTop = area.Bottom - Math.Min(area.Height, wr.Height);
        var px = Limit(x, area.Left, maxLeft);
        var py = Limit(y, area.Top, maxTop);
        MoveWindow(handle, px, py);
    }

    public static void SetWindowSize(IntPtr handle, int width, int height)
    {
        if (handle == IntPtr.Zero || width <= 0 || height <= 0)
        {
            return;
        }

        NativeWin32.GetWindowRect(handle, out var r);
        NativeWin32.SetWindowPos(
            handle,
            IntPtr.Zero,
            r.Left,
            r.Top,
            width,
            height,
            NativeWin32.SWP_DRAWFRAME | NativeWin32.SWP_NOACTIVATE);
    }

    public static void ApplyNoActivate(IntPtr handle, bool noActivate)
    {
        if (handle == IntPtr.Zero || !noActivate)
        {
            return;
        }

        var ex = (uint)NativeWin32.GetWindowLongPtr(handle, NativeWin32.GWL_EXSTYLE).ToInt64();
        ex |= NativeWin32.WS_EX_NOACTIVATE;
        if (IntPtr.Size == 8)
        {
            NativeWin32.SetWindowLongPtr(handle, NativeWin32.GWL_EXSTYLE, new IntPtr((long)ex));
        }
        else
        {
            NativeWin32.SetWindowLong(handle, NativeWin32.GWL_EXSTYLE, (int)ex);
        }
    }

    public static void ApplyMousePenetration(IntPtr handle, bool enable)
    {
        if (handle == IntPtr.Zero || !enable)
        {
            return;
        }

        var ex = (uint)NativeWin32.GetWindowLongPtr(handle, NativeWin32.GWL_EXSTYLE).ToInt64();
        ex |= NativeWin32.WS_EX_TRANSPARENT;
        if (IntPtr.Size == 8)
        {
            NativeWin32.SetWindowLongPtr(handle, NativeWin32.GWL_EXSTYLE, new IntPtr((long)ex));
        }
        else
        {
            NativeWin32.SetWindowLong(handle, NativeWin32.GWL_EXSTYLE, (int)ex);
        }
    }

    public static void ApplyTopmostHwnd(IntPtr handle, bool topmost)
    {
        if (handle == IntPtr.Zero || !topmost)
        {
            return;
        }

        const int HWND_TOPMOST = -1;
        NativeWin32.SetWindowPos(
            handle,
            new IntPtr(HWND_TOPMOST),
            0,
            0,
            0,
            0,
            NativeWin32.SWP_NOMOVE | NativeWin32.SWP_NOSIZE | NativeWin32.SWP_NOACTIVATE | NativeWin32.SWP_SHOWWINDOW);
    }

    private static void MoveWindowIntoCursorScreen(IntPtr handle) =>
        MoveWindowInToScreen(handle);

    private static void MoveWindowToMouseAround(IntPtr handle, bool limitInScreen = true)
    {
        var cursor = Control.MousePosition;
        var workingArea = Screen.FromPoint(cursor).WorkingArea;
        NativeWin32.GetWindowRect(handle, out var wr);
        var x = cursor.X - wr.Width / 2;
        var y = cursor.Y - wr.Height / 2;
        if (limitInScreen)
        {
            var maxLeft = workingArea.Right - wr.Width;
            var maxTop = workingArea.Bottom - wr.Height;
            x = Limit(x, workingArea.Left, maxLeft);
            y = Limit(y, workingArea.Top, maxTop);
        }

        MoveWindow(handle, x, y);
    }

    private static void MoveWindowToMouseBottomRight(IntPtr handle, int offsetX = 0, int offsetY = 0)
    {
        var mouse = Control.MousePosition;
        var workingArea = Screen.FromPoint(mouse).WorkingArea;
        NativeWin32.GetWindowRect(handle, out var wr);
        var (left, top) = ComputeMouseAdaptiveCornerTopLeft(
            mouse.X,
            mouse.Y,
            offsetX,
            offsetY,
            wr.Width,
            wr.Height,
            workingArea);
        var maxLeft = workingArea.Right - wr.Width;
        var maxTop = workingArea.Bottom - wr.Height;
        left = Limit(left, workingArea.Left, maxLeft);
        top = Limit(top, workingArea.Top, maxTop);
        MoveWindow(handle, left, top);
    }

    private static (int left, int top) ComputeMouseAdaptiveCornerTopLeft(
        int mouseX,
        int mouseY,
        int offsetX,
        int offsetY,
        int winW,
        int winH,
        Rectangle workingArea)
    {
        (int dx, int dy)[] shifts =
        {
            (0, 0),
            (0, -winH),
            (-winW, -winH),
            (-winW, 0),
        };

        foreach (var (dx, dy) in shifts)
        {
            var left = mouseX - offsetX + dx;
            var top = mouseY - offsetY + dy;
            if (Fits(workingArea, left, top, winW, winH))
            {
                return (left, top);
            }
        }

        return (mouseX - offsetX, mouseY - offsetY);
    }

    private static bool Fits(Rectangle wa, int left, int top, int w, int h) =>
        left >= wa.Left
        && top >= wa.Top
        && left + w <= wa.Right
        && top + h <= wa.Bottom;

    private static void MoveWindow(IntPtr handle, int x, int y) =>
        NativeWin32.SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, NativeWin32.SWP_NOSIZE | NativeWin32.SWP_DRAWFRAME | NativeWin32.SWP_NOACTIVATE);

    private static void MoveWindow(IntPtr handle, System.Drawing.Point p) => MoveWindow(handle, p.X, p.Y);

    private static Rectangle GetWorkingAreaByCursor() =>
        Screen.FromPoint(Control.MousePosition).WorkingArea;

    private static int Limit(int v, int min, int max)
    {
        if (v < min)
        {
            return min;
        }

        return v > max ? max : v;
    }
}
