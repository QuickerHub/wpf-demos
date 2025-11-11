using System;
using System.Windows;
using QuickerCodeEditor.View;
using QuickerCodeEditor.View.CodeEditor;

namespace QuickerCodeEditor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private WindowAttachedPopup? _testPopup;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            CreateTestPopup();
        }

        private void CreateTestPopup()
        {
            _testPopup = new WindowAttachedPopup
            {
                TargetWindow = this,
                WindowPlacement = WindowPlacement.Left,
                OffsetX = 10,
                OffsetY = 0,
                Width = 300,
                Height = 400,
                IsOpen = false,
                StaysOpen = true
            };

            var border = new System.Windows.Controls.Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5)
            };

            // Create CodeEditorStateControl
            var stateControl = new CodeEditorStateControl();
            
            // Add some test states
            stateControl.ViewModel.States.Add(new CodeEditorState
            {
                Name = "State 1",
                Expression = "$=1+1"
            });
            
            stateControl.ViewModel.States.Add(new CodeEditorState
            {
                Name = "State 2",
                Expression = "$=2+2"
            });
            
            stateControl.ViewModel.States.Add(new CodeEditorState
            {
                Name = "State 3",
                Expression = "$=3+3"
            });

            border.Child = stateControl;
            _testPopup.Child = border;

            // Open popup if window is already active
            if (this.IsActive)
            {
                _testPopup.IsOpen = true;
            }
        }

        private void OpenCodeEditor_Click(object sender, RoutedEventArgs e)
        {
            var codeEditor = new CodeEditorWrapper();
            codeEditor.TheWindow.Show();
        }
    }
}

