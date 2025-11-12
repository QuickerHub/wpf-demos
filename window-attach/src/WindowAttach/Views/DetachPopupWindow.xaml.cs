using System;
using System.Windows;
using System.Windows.Interop;
using WindowAttach;

namespace WindowAttach.Views
{
    /// <summary>
    /// Popup window for detaching window attachment
    /// </summary>
    public partial class DetachPopupWindow : Window
    {
        public IntPtr Window1Handle { get; set; }
        public IntPtr Window2Handle { get; set; }

        public DetachPopupWindow(IntPtr window1Handle, IntPtr window2Handle)
        {
            InitializeComponent();
            Window1Handle = window1Handle;
            Window2Handle = window2Handle;
            
            // Set initial position - will be updated by attachment service
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        private void DetachButton_Click(object sender, RoutedEventArgs e)
        {
            // Unregister the main attachment (this will also destroy this popup)
            Runner.Unregister(Window1Handle, Window2Handle);
            Close();
        }
    }
}

