using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using DependencyPropertyGenerator;

namespace BatchRenameTool.Controls
{
    /// <summary>
    /// Button control that shows a popup with custom content
    /// </summary>
    [ContentProperty(nameof(PopupContent))]
    [DependencyProperty<object>("Header")]
    [DependencyProperty<object>("PopupContent")]
    [DependencyProperty<Thickness>("ButtonPadding")]
    [DependencyProperty<double>("PopupMinHeight", DefaultValue = 100.0)]
    [DependencyProperty<double>("PopupMinWidth", DefaultValue = 100.0)]
    public partial class PopupButton : UserControl
    {
        private Popup? _popup;

        public PopupButton()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_popup == null)
            {
                _popup = this.FindName("PART_Popup") as Popup;
            }

            if (_popup != null)
            {
                _popup.IsOpen = !_popup.IsOpen;
            }
        }

        /// <summary>
        /// Close the popup
        /// </summary>
        public void ClosePopup()
        {
            if (_popup == null)
            {
                _popup = this.FindName("PART_Popup") as Popup;
            }

            if (_popup != null)
            {
                _popup.IsOpen = false;
            }
        }
    }
}
