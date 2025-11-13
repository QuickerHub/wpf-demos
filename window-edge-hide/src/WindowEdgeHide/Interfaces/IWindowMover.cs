using WindowEdgeHide.Models;

namespace WindowEdgeHide.Interfaces
{
    /// <summary>
    /// Interface for window movement operations
    /// </summary>
    public interface IWindowMover
    {
        /// <summary>
        /// Move window to specified position
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="x">Target X position</param>
        /// <param name="y">Target Y position</param>
        /// <param name="width">Window width</param>
        /// <param name="height">Window height</param>
        void MoveWindow(System.IntPtr windowHandle, int x, int y, int width, int height);
    }
}

