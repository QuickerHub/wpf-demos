using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CeaViewRunner.ViewModels;

namespace CeaViewRunner.Views;

public partial class TimeWindowPlus : Window
{
    public TimeWindowPlus() : this(null)
    {
    }

    public TimeWindowPlus(TimeWindowViewModel? vm)
    {
        InitializeComponent();
        ViewModel = vm ?? new TimeWindowViewModel();
        DataContext = ViewModel;
        Closed += (_, _) => Timer?.Stop();
        MouseWheel += Win_MouseWheel;
        Loaded += (_, _) => Timer?.Start();
        MouseDoubleClick += (_, _) => Close();
    }

    public TimeWindowViewModel ViewModel { get; set; }

    public DispatcherTimer? Timer { get; set; }

    private void Win_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta != 0)
        {
            AdjustBlockSize(e.Delta / 60);
        }
    }

    public void AdjustBlockSize(int delta)
    {
        ClockBlock.FontSize = Math.Max(10, ClockBlock.FontSize + delta);
    }
}
