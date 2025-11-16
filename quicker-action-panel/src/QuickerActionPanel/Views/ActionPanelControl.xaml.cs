using QuickerActionPanel.ViewModels;
using System.Windows.Controls;

namespace QuickerActionPanel.Views
{
    /// <summary>
    /// Interaction logic for ActionPanelControl.xaml
    /// </summary>
    public partial class ActionPanelControl : UserControl
    {
        private readonly ActionItemListViewModel _viewModel;

        public ActionItemListViewModel ViewModel => _viewModel;

        public ActionPanelControl()
        {
            InitializeComponent();
            _viewModel = new ActionItemListViewModel();
            DataContext = this;
        }
    }
}
