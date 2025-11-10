using static Windows.Win32.PInvoke;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using BetterClip.Helpers;
using System.Text;

namespace BetterClip.Win32;

/// <summary>
/// 窗口属性类，用于获取和操作窗口的各种属性
/// 属性将在首次访问时获取并缓存，以提高性能
/// 缓存的属性包括：窗口类名(ClassName)、进程文件路径(ExeFilePath)、进程名(ProcessName)
/// 注意：窗口标题(Title)不会被缓存，因为它可能会改变
/// </summary>
public class WinProperty
{
    public nint Handle = default;
    internal HWND HWnd => new(Handle);
    
    private readonly ILogger _logger = Launcher.GetLogger<WinProperty>();
    
    /// <summary>
    /// 安全地执行函数，并处理可能的异常
    /// </summary>
    private T SafeExecute<T>(Func<T> action, string errorMessage, T defaultValue)
    {
        if (Handle == default) return defaultValue;
        
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage);
            return defaultValue;
        }
    }
    
    // 缓存字段
    private string? _className;
    private string? _exeFilePath;
    private string? _processName;
    private Process? _process;
    
    /// <summary>
    /// 获取与窗口关联的进程，并缓存结果
    /// GetProcessById 方法耗时非常长, 有 13ms
    /// </summary>
    private Process? GetProcess()
    {
        if (_process == null && Handle != IntPtr.Zero)
        {
            try
            {
                uint pid;
                bool result;

                unsafe
                {
                    result = GetWindowThreadProcessId(new HWND(Handle), &pid) != 0;
                }

                if (result)
                {
                    _process = Process.GetProcessById((int)pid);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get process by ID");
            }
        }
        return _process;
    }
    
    /// <summary>
    /// 获取窗口的类名
    /// </summary>
    public string ClassName 
    { 
        get
        {
            if (_className == null && Handle != IntPtr.Zero)
            {
                _className = SafeExecute(
                    () => NativeMethods.GetClassNameOfWindow(Handle),
                    "Error getting window class name",
                    "");
            }
            return _className ?? "";
        }
    }
    
    public string ShortClassName => Regex.Match(ClassName, @"\w*").Value;
    
    /// <summary>
    /// 获取与窗口关联的进程的可执行文件路径
    /// </summary>
    public string ExeFilePath
    {
        get
        {
            if (_exeFilePath == null && Handle != IntPtr.Zero)
            {
                _exeFilePath = SafeExecute(
                    () => {
                        var process = GetProcess();
                        return process?.MainModule?.FileName ?? "";
                    },
                    "Error getting exe file path",
                    "");
            }
            return _exeFilePath ?? "";
        }
    }
    
    /// <summary>
    /// 获取与窗口关联的进程名称
    /// </summary>
    public string ProcessName
    {
        get
        {
            if (_processName == null && Handle != IntPtr.Zero)
            {
                _processName = SafeExecute(
                    () => {
                        var process = GetProcess();
                        return process?.ProcessName ?? "";
                    },
                    "Error getting process name",
                    "");
            }
            return _processName ?? "";
        }
    }
    
    /// <summary>
    /// 获取或设置窗口标题
    /// 注意：此属性不会被缓存，每次访问都会重新获取
    /// </summary>
    public string Title
    {
        get 
        {
            return SafeExecute(
                () => NativeMethods.GetWindowTitle(Handle),
                "Error getting window title",
                "");
        }
        set 
        {
            if (Handle != IntPtr.Zero)
            {
                try
                {
                    SetWindowText(new(Handle), value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting window title");
                }
            }
        }
    }
    
    public string GetShortTitle()
    {
        var title = Title;
        if (IsExplorerWindow && title.Length > 10)
            return Path.GetFileName(title);
        else
            return title;
    }

    public static WinProperty Default = new();
    public bool IsDefault => this == Default;
    private WinProperty() { }

    private static readonly ConcurrentDictionary<nint, WinProperty> _wps = new();
    public static WinProperty GetTemp(nint handle) => handle == IntPtr.Zero ? Default : new WinProperty(handle);
    public static WinProperty Get(nint handle)
    {
        if (handle == IntPtr.Zero) return Default;
        return _wps.GetOrAdd(handle, new WinProperty(handle));
        //return _wps.FindOrAdd(w => w.Handle == handle, () => new WinProperty(handle));
    }
    public static WinProperty GetForeground() => Get(GetForegroundWindow());
    public static WinProperty FromCursor() => Get(NativeMethods.GetWindowUnderCursor());
    public static List<WinProperty> FromClassName(string className)
    {
        var list = new List<WinProperty>();
        EnumWindows((hwnd, l) =>
        {
            var winp = GetTemp(hwnd);
            if (winp.ClassName == className)
            {
                list.Add(winp);
            }
            return true;
        }, default);
        return list;
    }
    private WinProperty(nint handle)
    {
        Handle = handle;
    }
    public Dictionary<string, object> GetSummary()
    {
        var dict = new Dictionary<string, object>()
        {
            ["进程路径"] = ExeFilePath,
            ["进程名"] = ProcessName,
            ["类名"] = ClassName,
        };
        if (IsCurrentProcess)
        {
            //var win = GetWPFWindow();
            //if (win != null)
            //{
            //    var type = win.GetType();
            //    dict["C#类名"] = type.Name;
            //    dict["C#完整类名"] = type.FullName;
            //    dict["C#限定名"] = $"{type.FullName}, {type.Assembly.GetName().Name}";
            //}
        }
        dict["标题"] = Title;
        dict["句柄"] = Handle;
        dict["位置"] = Rect;
        return dict;
    }

    public override string ToString() => ProcessName;
    public string ExeName => Path.GetFileName(ExeFilePath);
    public bool IsHwndWrapperClass => ClassName.StartsWith("HwndWrapper");

    /// <summary>
    /// 那种guid随机类名的
    /// </summary>
    public bool IsRandomClassName => ClassName.Contains('-') && Regex.IsMatch(ClassName, @"[0-9A-Fa-f\-]{36}");

    #region 窗口属性
    public bool IsWindow => IsWindow(HWnd);

    public Rect Rect
    {
        get
        {
            var r = NativeMethods.GetWindowRect1(Handle);
            return new Rect(r.left, r.top, r.right - r.left, r.bottom - r.top);
        }
    }

    public bool EnumFilter => IsVisible && !string.IsNullOrEmpty(Title) && !IsToolWindow;

    public bool IsTopLevel => GetTopWindow(HWnd) == default;

    public nint GetRootWindow()
    {
        var ancestor = GetAncestor(HWnd, GET_ANCESTOR_FLAGS.GA_ROOT);
        return ancestor == default || ancestor == GetDesktopWindow() ? Handle : ancestor;
    }

    public bool IsEqualOrParentOf(nint target)
    {
        if (target == IntPtr.Zero) return false;
        if (Handle == target) return true;
        return IsEqualOrParentOf(GetParent(new(target)));
    }

    public bool IsVisible => IsWindowVisible(HWnd);

    /// <summary>
    /// 设置为true过后就不在alt+tab中显示
    /// </summary>
    public bool IsToolWindow
    {
        get => CheckExStyle(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
        set => SetExStyle(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW, value);
    }

    public bool IsForeground => GetForegroundWindow() == Handle;
    public bool Minimized
    {
        get => CheckStyle(WINDOW_STYLE.WS_MINIMIZE);
        set
        {
            if (value) ShowWindow(HWnd, SHOW_WINDOW_CMD.SW_MINIMIZE);
            else ShowWindow(HWnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
        }
    }
    public bool Maximized
    {
        get => CheckStyle(WINDOW_STYLE.WS_MAXIMIZE);
        set
        {
            if (value)
            {
                ShowWindow(HWnd, SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED);
                SetForegroundWindow(HWnd);
            }
            else ShowWindow(HWnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
        }
    }
    public bool CanMaximize
    {
        get => CheckStyle(WINDOW_STYLE.WS_MAXIMIZEBOX);
        set => SetStyle(WINDOW_STYLE.WS_MAXIMIZEBOX, value);
    }
    private bool CheckStyle(WINDOW_STYLE style) => ((WINDOW_STYLE)GetWindowLong(HWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE)).HasFlag(style);

    internal void SetStyle(WINDOW_STYLE flag, bool value)
    {
        var get = (WINDOW_STYLE)GetWindowLong(HWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        var set = value ? get | flag : get & ~flag;
        SetWindowLong(HWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)set);
    }
    private bool CheckExStyle(WINDOW_EX_STYLE stylesEx) => ((WINDOW_EX_STYLE)GetWindowLong(HWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE)).HasFlag(stylesEx);

    internal void SetExStyle(WINDOW_EX_STYLE flag, bool value)
    {
        var get = (WINDOW_EX_STYLE)GetWindowLong(HWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        var set = value ? get | flag : get & ~flag;
        SetWindowLong(HWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)set);
    }

    /// <summary>
    /// 鼠标穿透
    /// </summary>
    public bool MousePenetration
    {
        get => CheckExStyle(WINDOW_EX_STYLE.WS_EX_TRANSPARENT);
        set => SetExStyle(WINDOW_EX_STYLE.WS_EX_TRANSPARENT, value);
    }

    /// <summary>
    /// 这里的 topmost 设置过后，就可以在所有的虚拟桌面上显示
    /// </summary>
    public bool Topmost
    {
        get => CheckExStyle(WINDOW_EX_STYLE.WS_EX_TOPMOST);
        set
        {
            var flag = SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW;
            if (value)
            {
                SetWindowPos(HWnd, new(new IntPtr(-1)), 0, 0, 0, 0, flag);
            }
            else
            {
                SetWindowPos(HWnd, new(new IntPtr(-2)), 0, 0, 0, 0, flag);
            }
        }
    }


    /// <summary>
    /// 是否启用无焦点模式
    /// </summary>
    public bool NoActivate
    {
        get => CheckExStyle(WINDOW_EX_STYLE.WS_EX_NOACTIVATE);
        set => SetExStyle(WINDOW_EX_STYLE.WS_EX_NOACTIVATE, value);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetLayeredWindowAttributes(nint hwnd, int crKey, int bAlpha, int dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetLayeredWindowAttributes(nint hwnd, out int crKey, out int bAlpha, out int dwFlags);

    public bool SetWindowOpacity(nint hWnd, int bAlpha)
    {
        SetExStyle(WINDOW_EX_STYLE.WS_EX_LAYERED, true);
        byte setAlpha = (byte)(bAlpha < 0 ? 0 : bAlpha > 255 ? 255 : bAlpha);
        return SetLayeredWindowAttributes(hWnd, 0, setAlpha, 2);
    }

    public int GetWindowOpacity(nint hWnd)
    {
        GetLayeredWindowAttributes(hWnd, out _, out int alpha, out _);
        return alpha;
    }
    /// <summary>
    /// 0-255
    /// </summary>
    public int Opacity
    {
        get => GetWindowOpacity(Handle);
        set => SetWindowOpacity(Handle, value);
    }

    #endregion

    #region 窗口操作,进程操作
    public void Show()
    {
        if (!IsWindow) return;
        var flag = SHOW_WINDOW_CMD.SW_SHOW;
        if (CheckStyle(WINDOW_STYLE.WS_MINIMIZE))
            flag = SHOW_WINDOW_CMD.SW_SHOWNORMAL;
        ShowWindow(HWnd, flag);
        SetForegroundWindow(HWnd);
        //if (IsInQuicker)
        //    GetWPFWindow()?.Activate();
    }
    public void OpenOrShow(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            path = ExeFilePath;
        }
        if (IsForeground && !Minimized)
        {
            CommonHelper.OpenFileOrUrl(path);
        }
        else
        {
            try { Show(); }
            catch { }
        }
    }

    public void Close() => SendMessage(HWnd, 0x0010, new(0), IntPtr.Zero);

    public void SetAsForeground() => SetForegroundWindow(HWnd);
    //public void MoveTo(WindowLocations loc) => WindowHelper.MoveWindow(Handle, loc);
    //public void MoveToCenterScreen()
    //{
    //    Show();
    //    MoveTo(WindowLocations.CenterScreen);
    //}
    //public void CloseAllByClassName()
    //{
    //    foreach (var handle in WindowEnumerator.GetAllWindows())
    //    {
    //        try
    //        {
    //            var winp = GetTemp(handle);
    //            if (winp.ClassName == ClassName)
    //            {
    //                winp.Close();
    //            }
    //        }
    //        catch { }
    //    }
    //}
    public void KillAllProcessByName()
    {
        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            process.Kill();
        }
        if (IsExplorer)
        {
            //Process.Start("explorer.exe");
        }
    }
    public void KillProcess() => GetProcess()?.Kill();
    #endregion

    #region 系统，资源管理器
    public bool IsExplorerWindow => ClassName == "CabinetWClass" || ClassName == "CloverWidgitWin_0";
    public bool IsExplorer => ProcessName == "explorer";
    public bool IsDesktop => ClassName == "WorkerW";
    public bool IsWindowUiCoreWindow => ClassName == "Windows.UI.Core.CoreWindow";
    public bool IsTaskbar => ClassName == "Shell_TrayWnd";
    public bool IsSaveAsWindow => ClassName == "#32770";
    public bool IsApplicationFrameWindow => ClassName == "ApplicationFrameWindow";

    /// <summary>
    /// edge 窗口的非激活标签页
    /// </summary>
    public bool IsWindowsInternalShellTabProxyWindow => ClassName == "Windows.Internal.Shell.TabProxyWindow";

    #endregion

    #region 聊天软件
    public bool IsQQ => IsProcNameCheck();
    public bool IsTim => IsProcNameCheck();
    public bool IsTimOrQQ => ProcNameEqual("QQ", "Tim");
    public bool IsWeChat => IsProcNameCheck();
    public bool IsTimQQorWC => ProcNameEqual("QQ", "WeChat", "Tim");

    //public bool IsWeChatApp => ProcessName == "WeChatApp";
    public bool IsWeChatAppEx => IsProcNameCheck();
    #endregion

    #region 浏览器
    public bool Ischrome => IsProcNameCheck();
    public bool Ismsedge => IsProcNameCheck();
    public bool Isfirefox => IsProcNameCheck();
    public bool Isvivaldi => IsProcNameCheck();
    public bool Is360ChromeX => IsProcNameCheck();
    public bool Is360Chrome => IsProcNameCheck();
    public bool Is360se => IsProcNameCheck();
    public bool IsBrowser => Ischrome || Ismsedge || Isfirefox || Isvivaldi || Is360Chrome || Is360ChromeX || Is360se;
    //public bool Is360 => 
    #endregion

    #region 内部，quicker
    //public Window? GetWPFWindow() => WindowHelper.TryGetWindow(Handle);
    public bool IsCurrentProcess => ProcessName == Process.GetCurrentProcess().ProcessName;
    public bool IsInQuicker => ProcessName == "Quicker";
    private bool IsSupperMenuWindow => Title == "SupperMenuWindow";
    private bool IsFloatButtonWindow => Title == "FloatButtonWindow";
    private bool IsCircleMenuWindow => Title == "CircleMenuWindow";
    public bool IsQuickerIgnoreWindow => IsInQuicker && (IsSupperMenuWindow || IsFloatButtonWindow || IsCircleMenuWindow);
    #endregion

    #region office
    public bool IsExcel => ProcNameEqual("EXCEL");
    public bool IsWord => ProcNameEqual("WINWORD");
    public bool IsPPT => ProcNameEqual("POWERPNT");
    public bool IsOffice => IsExcel || IsWord || IsPPT;
    #endregion

    public bool ProcNameEqual(params string[] vs) => vs.Any(x => x.Equals(ProcessName, StringComparison.OrdinalIgnoreCase));
    private bool IsProcNameCheck([CallerMemberName] string? comp = null) => ProcessName.Equals(comp?.Substring(2), StringComparison.OrdinalIgnoreCase);
}
