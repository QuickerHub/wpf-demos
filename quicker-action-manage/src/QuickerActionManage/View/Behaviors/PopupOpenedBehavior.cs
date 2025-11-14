using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using HandyControl.Interactivity;

namespace QuickerActionManage.View.Behaviors
{
    public class PopupOpenedBehavior : Behavior<Popup>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Opened += AssociatedObject_Opened;
        }

        [DllImport("User32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        private void AssociatedObject_Opened(object sender, EventArgs e)
        {
            if (AssociatedObject is Popup popup)
            {
                var source = (HwndSource)PresentationSource.FromVisual(popup.Child);
                if (source != null)
                {
                    SetFocus(source.Handle);
                }
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Opened -= AssociatedObject_Opened;
            base.OnDetaching();
        }
    }
}

