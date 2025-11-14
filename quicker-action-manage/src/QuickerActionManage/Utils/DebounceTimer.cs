using System;
using System.Windows.Threading;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// Debounce timer for delaying action execution
    /// </summary>
    public class DebounceTimer
    {
        private readonly DispatcherTimer _timer;
        private readonly int _interval;
        
        public DebounceTimer(int minis = 300)
        {
            _timer = new DispatcherTimer();
            _interval = minis;
            _timer.Interval = TimeSpan.FromMilliseconds(minis);
            _timer.Tick += Timer_Tick;
        }

        private Action? _debounceAction = null;

        /// <summary>
        /// Execute action with debounce delay
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="delay">Whether to wait before first execution</param>
        public void DoAction(Action action, bool delay = false)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(_interval);

            if (delay && !_timer.IsEnabled)
            {
                _timer.Start();
            }

            if (_timer.IsEnabled)
            {
                _debounceAction = action;
            }
            else
            {
                action?.Invoke();
                _timer.Start();
            }
        }

        public void DoAction(Action action, int mini)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(mini);
            _timer.Start();
            _debounceAction = action;
        }

        private int idleTimes;
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_debounceAction == null)
            {
                idleTimes += _interval;
                if (idleTimes > 3000) _timer.Stop(); // Stop after 3 seconds of idle
            }
            _debounceAction?.Invoke();
            _debounceAction = null; // Clear after execution
        }
    }
}

