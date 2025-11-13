using System;
using System.Windows;
using System.Windows.Media;
using Windows.Win32.UI.WindowsAndMessaging;
using WindowEdgeHide.Interfaces;
using WindowEdgeHide.Utils;

namespace WindowEdgeHide.Implementations
{
    /// <summary>
    /// Ease-in-out window mover - moves window with smooth quart (4th power) ease-in-out animation
    /// Provides smooth acceleration at the start and smooth deceleration at the end
    /// Uses quart curve for more pronounced easing effect than cubic
    /// Supports cancellation of previous animation when a new one starts
    /// Uses CompositionTarget.Rendering for 60fps smooth animation
    /// </summary>
    public class EaseInOutWindowMover : IWindowMover
    {
        private const double AnimationDurationSeconds = 0.25; // Animation duration in seconds (250ms, slightly longer for better effect visibility)
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
        /// Ease-in-out quart easing function (more pronounced than cubic)
        /// Provides smooth acceleration and deceleration with more noticeable effect
        /// Formula: ease-in-out-quart
        /// </summary>
        /// <param name="t">Progress value from 0.0 to 1.0</param>
        /// <returns>Eased progress value from 0.0 to 1.0</returns>
        private static double EaseInOutQuart(double t)
        {
            // Clamp t to [0, 1]
            t = Math.Max(0.0, Math.Min(1.0, t));
            
            // Quart (4th power) ease-in-out formula - more pronounced curve
            return t < 0.5
                ? 8 * t * t * t * t
                : 1 - Math.Pow(-2 * t + 2, 4) / 2;
        }

        /// <summary>
        /// Move window to specified position with ease-in-out animation
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
                        double linearProgress = Math.Min(elapsed / AnimationDurationSeconds, 1.0);

                        if (linearProgress >= 1.0)
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
                            // Apply ease-in-out quart easing (more pronounced than cubic)
                            double easedProgress = EaseInOutQuart(linearProgress);
                            
                            // Interpolate with eased progress
                            int currentX = state.StartX + (int)((state.TargetX - state.StartX) * easedProgress);
                            int currentY = state.StartY + (int)((state.TargetY - state.StartY) * easedProgress);
                            int currentWidth = state.StartWidth + (int)((state.TargetWidth - state.StartWidth) * easedProgress);
                            int currentHeight = state.StartHeight + (int)((state.TargetHeight - state.StartHeight) * easedProgress);

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

