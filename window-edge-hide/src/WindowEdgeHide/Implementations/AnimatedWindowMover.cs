using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Win32.UI.WindowsAndMessaging;
using WindowEdgeHide.Interfaces;
using WindowEdgeHide.Utils;

namespace WindowEdgeHide.Implementations
{
    /// <summary>
    /// Animated window mover - moves window with linear animation
    /// Supports cancellation of previous animation when a new one starts
    /// Uses CompositionTarget.Rendering for 60fps smooth animation
    /// </summary>
    public class AnimatedWindowMover : IWindowMover
    {
        private const double AnimationDurationSeconds = 0.2; // Animation duration in seconds (200ms)
        private EventHandler? _currentAnimationHandler;
        private readonly object _lockObject = new object();
        private AnimationState? _currentAnimationState;

        private class AnimationState
        {
            public IntPtr WindowHandle { get; set; }
            public int StartX { get; set; }
            public int StartY { get; set; }
            public int StartWidth { get; set; }
            public int StartHeight { get; set; }
            public int TargetX { get; set; }
            public int TargetY { get; set; }
            public int TargetWidth { get; set; }
            public int TargetHeight { get; set; }
            public DateTime StartTime { get; set; }
        }

        /// <summary>
        /// Move window to specified position with linear animation
        /// Cancels any ongoing animation before starting a new one
        /// </summary>
        public void MoveWindow(IntPtr windowHandle, int targetX, int targetY, int targetWidth, int targetHeight)
        {
            // Cancel any ongoing animation
            CancelCurrentAnimation();

            var currentRect = WindowHelper.GetWindowRect(windowHandle);
            if (currentRect == null)
            {
                // If we can't get current position, use direct move
                WindowHelper.SetWindowPos(windowHandle, targetX, targetY, targetWidth, targetHeight,
                    SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                return;
            }

            int startX = currentRect.Value.Left;
            int startY = currentRect.Value.Top;
            int startWidth = currentRect.Value.Width;
            int startHeight = currentRect.Value.Height;

            // If already at target position, no animation needed
            if (startX == targetX && startY == targetY && startWidth == targetWidth && startHeight == targetHeight)
            {
                return;
            }

            lock (_lockObject)
            {
                // Create animation state
                var state = new AnimationState
                {
                    WindowHandle = windowHandle,
                    StartX = startX,
                    StartY = startY,
                    StartWidth = startWidth,
                    StartHeight = startHeight,
                    TargetX = targetX,
                    TargetY = targetY,
                    TargetWidth = targetWidth,
                    TargetHeight = targetHeight,
                    StartTime = DateTime.Now
                };

                _currentAnimationState = state;

                // Create animation handler
                EventHandler? animationHandler = null;
                animationHandler = (sender, e) =>
                {
                    lock (_lockObject)
                    {
                        // Check if this animation was cancelled
                        if (_currentAnimationState != state || _currentAnimationHandler != animationHandler)
                        {
                            CompositionTarget.Rendering -= animationHandler;
                            if (_currentAnimationHandler == animationHandler)
                            {
                                _currentAnimationHandler = null;
                                _currentAnimationState = null;
                            }
                            return;
                        }

                        var elapsed = (DateTime.Now - state.StartTime).TotalSeconds;
                        double progress = Math.Min(elapsed / AnimationDurationSeconds, 1.0);

                        if (progress >= 1.0)
                        {
                            // Final position - ensure exact target position
                            WindowHelper.SetWindowPos(state.WindowHandle, state.TargetX, state.TargetY, 
                                state.TargetWidth, state.TargetHeight,
                                SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                            
                            CompositionTarget.Rendering -= animationHandler;
                            if (_currentAnimationHandler == animationHandler)
                            {
                                _currentAnimationHandler = null;
                                _currentAnimationState = null;
                            }
                        }
                        else
                        {
                            // Linear interpolation
                            int currentX = state.StartX + (int)((state.TargetX - state.StartX) * progress);
                            int currentY = state.StartY + (int)((state.TargetY - state.StartY) * progress);
                            int currentWidth = state.StartWidth + (int)((state.TargetWidth - state.StartWidth) * progress);
                            int currentHeight = state.StartHeight + (int)((state.TargetHeight - state.StartHeight) * progress);

                            WindowHelper.SetWindowPos(state.WindowHandle, currentX, currentY, currentWidth, currentHeight,
                                SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                        }
                    }
                };

                _currentAnimationHandler = animationHandler;
                CompositionTarget.Rendering += animationHandler;
            }
        }

        /// <summary>
        /// Cancel any ongoing animation
        /// </summary>
        private void CancelCurrentAnimation()
        {
            lock (_lockObject)
            {
                if (_currentAnimationHandler != null)
                {
                    CompositionTarget.Rendering -= _currentAnimationHandler;
                    _currentAnimationHandler = null;
                    _currentAnimationState = null;
                }
            }
        }
    }
}

