using System.Windows;
using System.Windows.Input;
using BatchRenameTool.Controls;
using BatchRenameTool.Models;

namespace BatchRenameTool.Windows
{
    /// <summary>
    /// Interaction logic for PatternHistoryWindow.xaml
    /// </summary>
    public partial class PatternHistoryWindow : Window
    {
        public PatternHistoryItem? SelectedPattern { get; private set; }

        public PatternHistoryWindow(PatternHistoryConfig config)
        {
            InitializeComponent();
            PatternListControl.SetPatterns(config);
            PatternListControl.PatternSelected += PatternListControl_PatternSelected;
            
            // Register ESC key to close window (use PreviewKeyDown to catch it before other controls)
            PreviewKeyDown += PatternHistoryWindow_PreviewKeyDown;
        }

        private void PatternHistoryWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                SelectedPattern = null;
                DialogResult = false;
                Close();
            }
        }

        private void PatternListControl_PatternSelected(object? sender, PatternHistoryItem item)
        {
            SelectedPattern = item;
            DialogResult = true;
            Close();
        }
    }
}
