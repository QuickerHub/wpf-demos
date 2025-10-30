using System.Runtime.InteropServices;

class NativeMethods
{
    // 导入 Windows API 函数
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern int EnumWindows(EnumWindowsProc ewp, int lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

    // 获取窗口标题的长度
    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    // 获取第一个标题以指定前缀开头的窗口句柄
    private static IntPtr FindWindowByTitleStartsWith(string titlePrefix)
    {
        IntPtr matchingWindowHandle = IntPtr.Zero;

        EnumWindows((hWnd, lParam) =>
        {
            int length = GetWindowTextLength(hWnd);
            if (length > 0)
            {
                System.Text.StringBuilder windowText = new System.Text.StringBuilder(length + 1);
                GetWindowText(hWnd, windowText, windowText.Capacity);

                if (windowText.ToString().StartsWith(titlePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    matchingWindowHandle = hWnd;
                    return false; // 找到匹配的窗口后停止枚举
                }
            }

            return true;
        }, 0);

        return matchingWindowHandle;
    }
}



