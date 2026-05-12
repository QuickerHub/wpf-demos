using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CeaViewRunner.Infrastructure;

namespace CeaViewRunner.ViewModels;

public partial class TimeWindowViewModel : ViewModelBase
{
    public TimeWindowSkin Skin { get; set; } = new();

    public class TimeWindowSkin
    {
        public Brush Background { get; set; } = new SolidColorBrush(Color.FromArgb(0x31, 0, 0, 0));

        public string? FontFamily { get; set; }

        public Brush Foreground { get; set; } = Brushes.White;

        public double FontSize1 { get; set; } = 40;

        public double FontSize2 { get; set; } = 15;
    }

    [DisplayName("提示")]
    public string? Tooltip { get; set; }

    [ObservableProperty]
    private string? _clockText;

    [ObservableProperty]
    private string? _dayText;

    [ObservableProperty]
    private bool _showDay;

    [ObservableProperty]
    private bool _success;
}

public static class TimeWindowRunner
{
    public enum TimerType
    {
        now,
        up,
        up_short,
        down,
    }

    public static Views.TimeWindowPlus CreateWindow(
        TimeWindowViewModel vm,
        string type,
        DateTime? timeEnd = null,
        string? tips = null,
        Action? timeOut = null,
        bool useLauner = false)
    {
        var win = new Views.TimeWindowPlus(vm);

        var timer = Enum.Parse(typeof(TimerType), type) switch
        {
            TimerType.now => CreateTimerNow(vm, useLauner),
            TimerType.up => CreateTimerUp(vm, TimeSpan.FromHours(2)),
            TimerType.up_short => CreateTimerUp(vm, TimeSpan.Zero),
            TimerType.down => CreateTimerDown(vm, timeEnd ?? default, () =>
            {
                vm.Success = true;
                win.Close();
                if (timeOut != null)
                {
                    System.Windows.Application.Current?.Dispatcher.InvokeAsync(timeOut);
                }
                else
                {
                    if (string.IsNullOrEmpty(tips))
                    {
                        tips = "倒计时结束";
                    }

                    MessageBox.Show(tips, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }),
            _ => throw new ArgumentException("不支持的时间类型:" + type),
        };

        win.Timer = timer;

        return win;
    }

    private static DispatcherTimer CreateTimerDown(TimeWindowViewModel vm, DateTime timeEnd, Action timeOut)
    {
        if (timeEnd <= DateTime.Now)
        {
            throw new ArgumentException("倒计时时间应该在当前时间之后");
        }

        vm.ShowDay = false;
        return CreateTimer(100, (s, e) =>
        {
            var t = timeEnd - DateTime.Now;
            if (t <= TimeSpan.Zero)
            {
                ((DispatcherTimer)s!).Stop();
                timeOut();
            }

            vm.ClockText = FormatTime(t);
        });
    }

    private static DispatcherTimer CreateTimerUp(TimeWindowViewModel vm, TimeSpan maxTime)
    {
        vm.ShowDay = false;
        var timeStart = DateTime.Now;
        if (maxTime.Hours == 0)
        {
            return CreateTimer(10, (s, e) =>
            {
                var t = DateTime.Now - timeStart;
                vm.ClockText = string.Format("{0:00}:{1:00}.{2:00}", t.Minutes, t.Seconds, t.Milliseconds / 10);
            });
        }

        return CreateTimer(100, (s, e) =>
        {
            var t = DateTime.Now - timeStart;
            vm.ClockText = FormatTime(t);
        });
    }

    public static string FormatTime(TimeSpan t)
    {
        if (t.Hours == 0)
        {
            return string.Format("{0:00}:{1:00}", t.Minutes, t.Seconds);
        }

        return string.Format("{0:00}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
    }

    private static DispatcherTimer CreateTimerNow(TimeWindowViewModel vm, bool useLunar)
    {
        vm.ShowDay = true;

        void SetNow(DateTime now)
        {
            vm.ClockText = now.ToString("HH:mm:ss");
            var timeNow = DateTime.Now.ToString("yyyy/MM/dd ddd");
            if (useLunar)
            {
                var lunarNow = LunarDateHelper.GetDateString(DateTime.Now);
                vm.DayText = timeNow + "\r\n" + lunarNow;
            }
            else
            {
                vm.DayText = timeNow;
            }
        }

        SetNow(DateTime.Now);

        return CreateTimer(100, (s, e) =>
        {
            var now = DateTime.Now;
            if (now.Millisecond < 200)
            {
                SetNow(now);
            }
        });
    }

    private static DispatcherTimer CreateTimer(int ms, EventHandler callback)
    {
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(ms),
        };
        callback.Invoke(null!, EventArgs.Empty);
        timer.Tick += callback;
        return timer;
    }
}
