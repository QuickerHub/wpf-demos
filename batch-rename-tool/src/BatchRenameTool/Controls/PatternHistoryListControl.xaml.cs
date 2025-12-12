using System;
using System.Windows;
using System.Windows.Controls;
using BatchRenameTool.Models;

namespace BatchRenameTool.Controls
{
    /// <summary>
    /// Interaction logic for PatternHistoryListControl.xaml
    /// </summary>
    public partial class PatternHistoryListControl : UserControl
    {
        public PatternHistoryListControl()
        {
            InitializeComponent();
            PatternListBox.MouseDoubleClick += PatternListBox_MouseDoubleClick;
        }

        /// <summary>
        /// Set the pattern history items
        /// </summary>
        public void SetPatterns(PatternHistoryConfig config)
        {
            PatternListBox.ItemsSource = config.Patterns;
        }

        /// <summary>
        /// Get selected pattern item
        /// </summary>
        public PatternHistoryItem? GetSelectedPattern()
        {
            return PatternListBox.SelectedItem as PatternHistoryItem;
        }

        /// <summary>
        /// Event raised when a pattern is double-clicked
        /// </summary>
        public event EventHandler<PatternHistoryItem>? PatternSelected;

        private void PatternListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PatternListBox.SelectedItem is PatternHistoryItem item)
            {
                PatternSelected?.Invoke(this, item);
            }
        }
    }
}
