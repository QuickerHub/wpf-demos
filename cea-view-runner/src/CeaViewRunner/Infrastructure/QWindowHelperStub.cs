using System;
using System.Windows;

namespace CeaViewRunner.Infrastructure;

/// <summary>
/// Optional Quicker host integration hook (original CeaQuicker QWindowHelper). No-op here.
/// </summary>
public static class QWindowHelperStub
{
    public static void SetCanUseQuicker(IntPtr hwnd, bool enable)
    {
        _ = hwnd;
        _ = enable;
    }
}
