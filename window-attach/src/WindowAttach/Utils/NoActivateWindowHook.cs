using System;
using System.Runtime.InteropServices;

namespace WindowAttach.Utils
{
    /// <summary>
    /// Window hook to prevent window activation when clicked
    /// Uses window subclassing to intercept WM_MOUSEACTIVATE message
    /// </summary>
    internal class NoActivateWindowHook : IDisposable
    {
        // Window procedure delegate
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        private readonly IntPtr _hWnd;
        private readonly IntPtr _originalWndProcPtr;
        private readonly WndProcDelegate _subclassWndProc;
        private GCHandle _subclassWndProcHandle;
        private bool _disposed;

        private const int GWLP_WNDPROC = -4;
        private const uint WM_MOUSEACTIVATE = 0x0021;
        private const uint MA_NOACTIVATE = 0x0003;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Create a hook to prevent window activation
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        public NoActivateWindowHook(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                throw new ArgumentException("Window handle cannot be zero", nameof(hWnd));

            _hWnd = hWnd;
            
            // Get the current window procedure pointer
            _originalWndProcPtr = GetWindowLongPtr(hWnd, GWLP_WNDPROC);
            
            if (_originalWndProcPtr == IntPtr.Zero)
                throw new InvalidOperationException("Failed to get original window procedure");

            // Create subclass window procedure and pin it to prevent garbage collection
            _subclassWndProc = SubclassWndProc;
            _subclassWndProcHandle = GCHandle.Alloc(_subclassWndProc);
            
            // Set the new window procedure
            var newWndProc = Marshal.GetFunctionPointerForDelegate(_subclassWndProc);
            SetWindowLongPtr(hWnd, GWLP_WNDPROC, newWndProc);
        }

        /// <summary>
        /// Subclass window procedure to intercept WM_MOUSEACTIVATE
        /// </summary>
        private IntPtr SubclassWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
        {
            // Intercept WM_MOUSEACTIVATE and return MA_NOACTIVATE to prevent activation
            if (uMsg == WM_MOUSEACTIVATE)
            {
                return new IntPtr(MA_NOACTIVATE);
            }

            // Call original window procedure for all other messages
            return CallWindowProc(_originalWndProcPtr, hWnd, uMsg, wParam, lParam);
        }

        /// <summary>
        /// Dispose the hook and restore original window procedure
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            if (_hWnd != IntPtr.Zero && _originalWndProcPtr != IntPtr.Zero)
            {
                try
                {
                    // Restore original window procedure
                    SetWindowLongPtr(_hWnd, GWLP_WNDPROC, _originalWndProcPtr);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }

            // Release the pinned delegate
            if (_subclassWndProcHandle.IsAllocated)
            {
                _subclassWndProcHandle.Free();
            }

            _disposed = true;
        }
    }
}

