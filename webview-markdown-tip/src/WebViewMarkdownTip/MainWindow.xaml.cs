using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Wpf;

namespace WebViewMarkdownTip
{
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private readonly MainViewModel _viewModel;
        private readonly DependencyPropertyDescriptor? _windowBackgroundDescriptor;

        public MainViewModel ViewModel => _viewModel;

        public MainWindow()
            : this(new MainViewModel())
        {
        }

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = this;

            // Set WebView2 default background color from HandyControl theme (WpfMonacoEditor MainWindow pattern)
            ApplyWebViewBackgroundFromTheme();

            _windowBackgroundDescriptor = DependencyPropertyDescriptor.FromProperty(
                Control.BackgroundProperty,
                typeof(Window));
            _windowBackgroundDescriptor?.AddValueChanged(this, OnWindowBackgroundChanged);

            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            Closed += MainWindow_Closed;

            Loaded += MainWindow_Loaded;

            _viewModel.CloseRequested += (_, _) => Close();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _windowBackgroundDescriptor?.RemoveValueChanged(this, OnWindowBackgroundChanged);
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General && e.Category != UserPreferenceCategory.Color)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(ApplyWebViewBackgroundFromTheme));
        }

        private void OnWindowBackgroundChanged(object? sender, EventArgs e)
        {
            ApplyWebViewBackgroundFromTheme();
        }

        /// <summary>
        /// Same source as WpfMonacoEditor: window <see cref="Background"/> (DefaultWindowBackgroundBrush), with fallback to app resources.
        /// </summary>
        private void ApplyWebViewBackgroundFromTheme()
        {
            if (Background != null)
            {
                WebViewControl.SetBackgroundColor((Brush)Background);
            }
            else
            {
                WebViewControl.SetBackgroundColorForHandyControl(Application.Current.Resources);
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyWebViewBackgroundFromTheme();
            await _viewModel.SetWebViewAsync(WebViewControl);
        }
    }
}
