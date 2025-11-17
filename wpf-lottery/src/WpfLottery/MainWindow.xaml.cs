using System.Windows;
using WpfLottery.Windows;

namespace WpfLottery
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenLotteryWindow_Click(object sender, RoutedEventArgs e)
        {
            var lotteryWindow = new LotteryWindow();
            lotteryWindow.Show();
        }
    }
}

