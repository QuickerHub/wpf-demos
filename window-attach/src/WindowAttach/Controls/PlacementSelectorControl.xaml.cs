using System;
using System.Windows;
using System.Windows.Controls;
using WindowAttach.Models;

namespace WindowAttach.Controls
{
    /// <summary>
    /// Control for selecting window placement with 12 buttons arranged around a rectangle
    /// </summary>
    public partial class PlacementSelectorControl : UserControl
    {
        /// <summary>
        /// Event raised when a placement is selected
        /// </summary>
        public event EventHandler<WindowPlacement>? PlacementSelected;

        public PlacementSelectorControl()
        {
            InitializeComponent();
        }

        private void PlacementButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WindowPlacement placement)
            {
                PlacementSelected?.Invoke(this, placement);
            }
        }
    }
}

