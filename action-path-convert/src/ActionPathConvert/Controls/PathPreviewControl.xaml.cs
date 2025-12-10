using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ActionPathConvert.Controls
{
    /// <summary>
    /// PathPreviewControl.xaml 的交互逻辑
    /// </summary>
    public partial class PathPreviewControl : UserControl
    {
        public static readonly DependencyProperty BeforeTextProperty =
            DependencyProperty.Register(
                nameof(BeforeText),
                typeof(string),
                typeof(PathPreviewControl),
                new PropertyMetadata(string.Empty, OnBeforeTextChanged));

        public static readonly DependencyProperty AfterTextProperty =
            DependencyProperty.Register(
                nameof(AfterText),
                typeof(string),
                typeof(PathPreviewControl),
                new PropertyMetadata(string.Empty, OnAfterTextChanged));

        public PathPreviewControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Before processing content
        /// </summary>
        public string BeforeText
        {
            get => (string)GetValue(BeforeTextProperty);
            set => SetValue(BeforeTextProperty, value);
        }

        /// <summary>
        /// After processing content
        /// </summary>
        public string AfterText
        {
            get => (string)GetValue(AfterTextProperty);
            set => SetValue(AfterTextProperty, value);
        }

        private static void OnBeforeTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PathPreviewControl control)
            {
                control.BeforeEditor.Text = e.NewValue as string ?? string.Empty;
            }
        }

        private static void OnAfterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PathPreviewControl control)
            {
                control.AfterEditor.Text = e.NewValue as string ?? string.Empty;
            }
        }
    }
}

