using System;
using WindowAttach.Models;
using WindowAttach.Services;

namespace WindowAttach.Extensions
{
    /// <summary>
    /// Extension methods for WindowAttachService
    /// </summary>
    public static class WindowAttachServiceExtensions
    {
        /// <summary>
        /// Register a window attachment with direct parameters
        /// </summary>
        /// <param name="service">Window attach service</param>
        /// <param name="window1Handle">Handle of the target window (window to follow)</param>
        /// <param name="window2Handle">Handle of the window to attach (window that follows)</param>
        /// <param name="placement">Placement position (default: RightTop)</param>
        /// <param name="offsetX">Horizontal offset (default: 0)</param>
        /// <param name="offsetY">Vertical offset (default: 0)</param>
        /// <param name="restrictToSameScreen">Whether to restrict window2 to the same screen as window1 (default: false)</param>
        /// <param name="autoOptimizePosition">Whether to automatically optimize position by trying all placement options and selecting the one with maximum visible area (default: true)</param>
        /// <param name="preventActivation">Whether to prevent window2 from being activated when clicked (default: false)</param>
        /// <param name="callbackAction">Callback action to execute when window1 is closed (default: null)</param>
        /// <returns>True if registered successfully, false if already registered</returns>
        public static bool Register(
            this WindowAttachService service,
            IntPtr window1Handle,
            IntPtr window2Handle,
            WindowPlacement placement = WindowPlacement.RightTop,
            double offsetX = 0,
            double offsetY = 0,
            bool restrictToSameScreen = false,
            bool autoOptimizePosition = true,
            bool preventActivation = false,
            Action? callbackAction = null)
        {
            var attachParams = new AttachParams
            {
                Placement = placement,
                OffsetX = offsetX,
                OffsetY = offsetY,
                RestrictToSameScreen = restrictToSameScreen,
                AutoOptimizePosition = autoOptimizePosition,
                PreventActivation = preventActivation,
                CallbackAction = callbackAction
            };

            return service.Register(window1Handle, window2Handle, attachParams);
        }

        /// <summary>
        /// Update attachment parameters with direct parameters
        /// </summary>
        /// <param name="service">Window attach service</param>
        /// <param name="window1Handle">Handle of the target window</param>
        /// <param name="window2Handle">Handle of the attached window</param>
        /// <param name="placement">Placement position (default: RightTop)</param>
        /// <param name="offsetX">Horizontal offset (default: 0)</param>
        /// <param name="offsetY">Vertical offset (default: 0)</param>
        /// <param name="restrictToSameScreen">Whether to restrict window2 to the same screen as window1 (default: false)</param>
        /// <param name="autoOptimizePosition">Whether to automatically optimize position by trying all placement options and selecting the one with maximum visible area (default: true)</param>
        /// <param name="preventActivation">Whether to prevent window2 from being activated when clicked (default: false)</param>
        /// <param name="callbackAction">Callback action to execute when window1 is closed (default: null)</param>
        /// <returns>True if updated successfully, false if not found</returns>
        public static bool Update(
            this WindowAttachService service,
            IntPtr window1Handle,
            IntPtr window2Handle,
            WindowPlacement placement = WindowPlacement.RightTop,
            double offsetX = 0,
            double offsetY = 0,
            bool restrictToSameScreen = false,
            bool autoOptimizePosition = true,
            bool preventActivation = false,
            Action? callbackAction = null)
        {
            var attachParams = new AttachParams
            {
                Placement = placement,
                OffsetX = offsetX,
                OffsetY = offsetY,
                RestrictToSameScreen = restrictToSameScreen,
                AutoOptimizePosition = autoOptimizePosition,
                PreventActivation = preventActivation,
                CallbackAction = callbackAction
            };

            return service.Update(window1Handle, window2Handle, attachParams);
        }
    }
}

