using System;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Alinko.Quicker.Services.Windows;

public enum WindowChangeType
{
    Foreground = 3,
    CaptureStart = 8,
    MinimizeEnd = 23,
}

public class ForegroundWindowChangedEventArgs(IntPtr hWnd, WindowChangeType changeType) : EventArgs
{
    public IntPtr HWnd { get; } = hWnd;
    public WindowChangeType ChangeType { get; } = changeType;
}

public delegate void ForegroundWindowChangedEventHandler(object sender, ForegroundWindowChangedEventArgs e);

public partial class ActiveWindowHook : HookBase
{
    public event ForegroundWindowChangedEventHandler? ForegroundWindowChanged;

    protected override uint[] EventList { get; } =
    [
        3, //EVENT_SYSTEM_FOREGROUND
        8, //EVENT_SYSTEM_CAPTURESTART
        23, //EVENT_SYSTEM_MINIMIZEEND
    ];

    /// <summary>
    /// call <see cref="HookBase.StartHook"/> to start hooking
    /// </summary>
    public ActiveWindowHook()
    {
        
    }
    internal override void WinEventProc(HWINEVENTHOOK hWinEventHook, uint @event, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        if (CheckEvent(@event))
        {
            var changeType = @event switch
            {
                3 => WindowChangeType.Foreground,
                8 => WindowChangeType.CaptureStart,
                23 => WindowChangeType.MinimizeEnd,
                _ => throw new NotImplementedException(),
            };
            ForegroundWindowChanged?.Invoke(this, new(hwnd, changeType));
        }
    }
}
