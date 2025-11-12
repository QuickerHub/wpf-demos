using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Windows.Markup;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using static Windows.Win32.PInvoke;

namespace WindowAttach.Controls
{
    /// <summary>
    /// Control that displays a toggle button with a popup that closes when focus is lost
    /// </summary>
    [DefaultProperty(nameof(Content))]
    [ContentProperty(nameof(Content))]
    [StyleTypedProperty(Property = nameof(ToggleButtonStyle), StyleTargetType = typeof(ToggleButton))]
    [TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
    [TemplatePart(Name = "PART_ToggleButton", Type = typeof(ToggleButton))]
    public class PopupButtonControl : ContentControl
    {
        private Popup? _popup;

        // Static hook for monitoring foreground and mouse capture events
        private static HWINEVENTHOOK? _staticHook;
        private static readonly HashSet<PopupButtonControl> _openPopups = new();
        private static readonly object _lockObject = new();
        
        // Windows Event Constants
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint EVENT_SYSTEM_CAPTURESTART = 0x0008;

        static PopupButtonControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupButtonControl), new FrameworkPropertyMetadata(typeof(PopupButtonControl)));
            
            // Initialize static hook
            InitializeStaticHook();
        }

        private static void InitializeStaticHook()
        {
            if (_staticHook != null)
                return;

            // Create WinEvent callback
            WINEVENTPROC winEventProc = (hWinEventHook, @event, hwnd, idObject, idChild, idEventThread, dwmsEventTime) =>
            {
                if (@event == EVENT_SYSTEM_FOREGROUND)
                {
                    // Close all open popups when foreground window changes
                    Application.Current?.Dispatcher.BeginInvoke(
                        new Action(() => CloseAllPopups()),
                        DispatcherPriority.Normal);
                }
                else if (@event == EVENT_SYSTEM_CAPTURESTART)
                {
                    // Only close popups if capture is not from our popup windows
                    Application.Current?.Dispatcher.BeginInvoke(
                        new Action(() => ClosePopupsIfNotFromCapture(hwnd)),
                        DispatcherPriority.Normal);
                }
            };

            // Set hook for system-wide events
            // Flags: 0 = WINEVENT_OUTOFCONTEXT (hook runs in separate thread)
            _staticHook = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND,
                EVENT_SYSTEM_CAPTURESTART,
                HMODULE.Null,
                winEventProc,
                0,
                0,
                0);
        }

        private static void ClosePopupsIfNotFromCapture(HWND captureWindowHandle)
        {
            lock (_lockObject)
            {
                // Check if the capture window belongs to any of our popups
                bool isFromOurPopup = false;
                foreach (var control in _openPopups)
                {
                    if (control._popup != null && control._popup.IsOpen)
                    {
                        // Get the popup's window handle
                        var popupHwndSource = PresentationSource.FromVisual(control._popup.Child) as System.Windows.Interop.HwndSource;
                        if (popupHwndSource != null && popupHwndSource.Handle == captureWindowHandle.Value)
                        {
                            isFromOurPopup = true;
                            break;
                        }
                        
                        // Also check if capture is from the popup's parent window (DetachPopupWindow)
                        var parentWindow = Window.GetWindow(control);
                        if (parentWindow != null)
                        {
                            var parentHwnd = new System.Windows.Interop.WindowInteropHelper(parentWindow).Handle;
                            if (parentHwnd == captureWindowHandle.Value)
                            {
                                isFromOurPopup = true;
                                break;
                            }
                        }
                    }
                }
                
                // Only close if capture is not from our popup
                if (!isFromOurPopup)
                {
                    CloseAllPopups();
                }
            }
        }

        private static void CloseAllPopups()
        {
            lock (_lockObject)
            {
                // Close all open popups
                foreach (var control in _openPopups.ToArray())
                {
                    control.IsChecked = false;
                }
                _openPopups.Clear();
            }
        }

        public PopupButtonControl()
        {
            // Load the style from the XAML resource dictionary using DLL name
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var resourceUri = new Uri($"/{assemblyName};component/Controls/PopupButtonControl.xaml", UriKind.Relative);
            var resourceDictionary = new ResourceDictionary
            {
                Source = resourceUri
            };
            this.Resources.MergedDictionaries.Add(resourceDictionary);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Unsubscribe from previous popup if exists
            if (_popup != null)
            {
                _popup.Opened -= Popup_Opened;
                _popup.Closed -= Popup_Closed;
            }

            // Get the popup from template
            _popup = GetTemplateChild("PART_Popup") as Popup;
            
            if (_popup != null)
            {
                _popup.Opened += Popup_Opened;
                _popup.Closed += Popup_Closed;
            }
        }

        private void Popup_Opened(object? sender, EventArgs e)
        {
            // Ensure popup and its content don't get focus
            if (_popup != null)
            {
                _popup.Focusable = false;
                if (_popup.Child != null)
                {
                    _popup.Child.Focusable = false;
                }
                
                // Register this popup as open
                lock (_lockObject)
                {
                    _openPopups.Add(this);
                }
            }
        }

        private void Popup_Closed(object? sender, EventArgs e)
        {
            // Unregister this popup when closed
            lock (_lockObject)
            {
                _openPopups.Remove(this);
            }
        }

        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header", 
            typeof(object), 
            typeof(PopupButtonControl), 
            new PropertyMetadata(null));

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            "IsChecked",
            typeof(bool),
            typeof(PopupButtonControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Style ToggleButtonStyle
        {
            get { return (Style)GetValue(ToggleButtonStyleProperty); }
            set { SetValue(ToggleButtonStyleProperty, value); }
        }

        public static readonly DependencyProperty ToggleButtonStyleProperty = DependencyProperty.Register(
            "ToggleButtonStyle", 
            typeof(Style), 
            typeof(PopupButtonControl), 
            new PropertyMetadata(null));
    }
}

