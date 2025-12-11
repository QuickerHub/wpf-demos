using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ActionPathConvert.ViewModels;

namespace ActionPathConvert.Controls
{
    /// <summary>
    /// ProcessResultControl.xaml 的交互逻辑
    /// </summary>
    public partial class ProcessResultControl : UserControl
    {
        private ProcessResultViewModel? _currentProcessResult;

        public static readonly DependencyProperty ProcessResultProperty =
            DependencyProperty.Register(
                nameof(ProcessResult),
                typeof(ProcessResultViewModel),
                typeof(ProcessResultControl),
                new PropertyMetadata(default(ProcessResultViewModel), OnProcessResultChanged));

        public ProcessResultControl()
        {
            InitializeComponent();
        }

        public ProcessResultViewModel? ProcessResult
        {
            get => (ProcessResultViewModel?)GetValue(ProcessResultProperty);
            set => SetValue(ProcessResultProperty, value);
        }

        private static void OnProcessResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProcessResultControl control)
            {
                // Unsubscribe from old result
                if (e.OldValue is ProcessResultViewModel oldResult)
                {
                    oldResult.PropertyChanged -= control.ProcessResult_PropertyChanged;
                }

                // Subscribe to new result
                if (e.NewValue is ProcessResultViewModel newResult)
                {
                    newResult.PropertyChanged += control.ProcessResult_PropertyChanged;
                }

                control._currentProcessResult = e.NewValue as ProcessResultViewModel;
                control.UpdateContent();
            }
        }

        private void ProcessResult_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ProcessResult == null)
                return;

            // Update FileEditor's FilePath when M3uFilePath changes
            if (e.PropertyName == nameof(ProcessResultViewModel.M3uFilePath))
            {
                FileEditor.FilePath = ProcessResult.M3uFilePath ?? "";
            }
            // NotFoundFilesText is now bound directly in XAML, no need to update manually
        }

        private void UpdateContent()
        {
            if (ProcessResult == null)
            {
                // FileEditor will handle empty path automatically
                FileEditor.FilePath = "";
                return;
            }

            // Update FileEditor's FilePath
            FileEditor.FilePath = ProcessResult.M3uFilePath ?? "";
            // NotFoundFilesText is now bound directly in XAML
        }
    }
}

