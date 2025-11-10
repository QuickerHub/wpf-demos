using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;
using Windows.Win32.UI.Accessibility;
using System;

namespace Alinko.Quicker.Services.Windows
{
    public abstract class HookBase : IDisposable
    {
        internal HWINEVENTHOOK _hook;

        internal readonly WINEVENTPROC _winEventProc;

        protected uint EventMin => EventList.Min();

        protected uint EventMax => EventList.Max();

        protected abstract uint[] EventList { get; }

        protected bool CheckEvent(uint @event) => EventList.Any(x => x == @event);

        /// <summary>
        /// call <see cref="StartHook"/> to start hooking
        /// </summary>
        public HookBase()
        {
            _winEventProc = new WINEVENTPROC(WinEventProc);
            //PInvoke.User32.WindowsEventHookType.eve
            //PInvoke.User32.WindowsEventHookType
        }

        internal abstract void WinEventProc(HWINEVENTHOOK hWinEventHook, uint @event, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime);

        //protected abstract void WinEventProc(HWINEVENTHOOK hWinEventHook, uint @event, IntPtr hwnd, int idObject, int idChild, int dwEventThread, uint dwmsEventTime);

        public void Dispose()
        {
            UnhookWinEvent(_hook);
            GC.SuppressFinalize(this);
        }

        public void StartHook()
        {
            _hook = SetWinEventHook(EventMin, EventMax, new HINSTANCE(IntPtr.Zero), _winEventProc, 0, 0, 0);
        }
    }
}
