using System.Windows;
using System.Windows.Input;
using WpfLottery.ViewModels;

namespace WpfLottery.Controls
{
    /// <summary>
    /// Interaction logic for LotteryControl.xaml
    /// </summary>
    public partial class LotteryControl : System.Windows.Controls.UserControl
    {
        private readonly LotteryViewModel _viewModel;

        public LotteryViewModel ViewModel => _viewModel;

        public LotteryControl()
        {
            InitializeComponent();
            _viewModel = new LotteryViewModel();
            DataContext = this;
        }

        private void NewItemTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as System.Windows.Controls.TextBox;
                if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    _viewModel.AddItemCommand.Execute(textBox.Text);
                    textBox.Text = "";
                    textBox.Focus();
                }
            }
        }
    }
}

