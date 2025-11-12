using System.Windows;
using QuickerStatisticsInfo.ViewModels;

namespace QuickerStatisticsInfo.View
{
    /// <summary>
    /// StatisticsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StatisticsWindow : Window
    {
        public StatisticsViewModel ViewModel { get; }

        public StatisticsWindow()
        {
            InitializeComponent();
            ViewModel = new StatisticsViewModel();
            DataContext = ViewModel;
        }

        /// <summary>
        /// Initialize window with user path
        /// </summary>
        /// <param name="userPath">User path</param>
        public void Initialize(string userPath)
        {
            ViewModel.Initialize(userPath);
        }

        /// <summary>
        /// Start collecting statistics asynchronously
        /// </summary>
        /// <param name="userPath">User path</param>
        public void StartCollecting(string userPath)
        {
            ViewModel.StartCollecting(userPath);
        }

        /// <summary>
        /// Load statistics data (for backward compatibility)
        /// </summary>
        /// <param name="result">Statistics result</param>
        public void LoadStatistics(StatisticsResult result)
        {
            ViewModel.LoadStatistics(result);
        }
    }
}

