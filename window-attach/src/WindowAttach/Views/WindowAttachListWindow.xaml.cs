using System.Windows;
using WindowAttach.ViewModels;

namespace WindowAttach.Views
{
    /// <summary>
    /// WindowAttachListWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WindowAttachListWindow : Window
    {
        public WindowAttachListWindow()
        {
            InitializeComponent();
            
            if (DataContext is WindowAttachListViewModel viewModel)
            {
                viewModel.CloseRequested += (s, e) => Close();
            }
        }
    }
}

