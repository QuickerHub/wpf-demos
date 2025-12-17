using System.Collections.Generic;
using System.Linq;
using System.Windows;
using QuickerActionManage.ViewModel;

namespace QuickerActionManage.View
{
    /// <summary>
    /// Interaction logic for ActionDataOptimizeWindow.xaml
    /// </summary>
    public partial class ActionDataOptimizeWindow : Window
    {
        private readonly ActionDataOptimizeViewModel _viewModel;
        private readonly IEnumerable<string>? _actionIds;

        /// <summary>
        /// Constructor for loading all actions
        /// </summary>
        public ActionDataOptimizeWindow() : this(null)
        {
        }

        /// <summary>
        /// Constructor for loading specified action IDs
        /// </summary>
        /// <param name="actionIds">Action IDs to load, or null to load all actions</param>
        public ActionDataOptimizeWindow(IEnumerable<string>? actionIds)
        {
            InitializeComponent();
            _viewModel = new ActionDataOptimizeViewModel();
            _actionIds = actionIds;
            DataContext = _viewModel;
            Loaded += ActionDataOptimizeWindow_Loaded;
        }

        private void ActionDataOptimizeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_actionIds != null)
            {
                _viewModel.LoadActionsByIds(_actionIds);
            }
            else
            {
                _viewModel.LoadActions();
            }
        }
    }
}

