using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StepsChanger;
using StepsChanger.Services;

namespace StepsChanger.Controls
{
    /// <summary>
    /// UserControl for changing Zepp steps
    /// </summary>
    public partial class StepsChangerControl : UserControl, INotifyPropertyChanged
    {
        private bool _isNotBusy = true;
        private string _statusMessage = "";
        private Brush _statusColor = Brushes.Black;

        public StepsChangerControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public bool IsNotBusy
        {
            get => _isNotBusy;
            set
            {
                _isNotBusy = value;
                OnPropertyChanged();
            }
        }

        public bool HasStatusMessage => !string.IsNullOrEmpty(_statusMessage);

        public Brush StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Get values from UI
            string account = AccountTextBox.Text?.Trim() ?? "";
            string password = PasswordBox.Password ?? "";
            string steps = StepsTextBox.Text?.Trim() ?? "";

            // Validate inputs
            if (string.IsNullOrWhiteSpace(account))
            {
                ShowStatus("请输入账号", Brushes.Red);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowStatus("请输入密码", Brushes.Red);
                return;
            }

            if (string.IsNullOrWhiteSpace(steps))
            {
                ShowStatus("请输入步数", Brushes.Red);
                return;
            }

            if (!int.TryParse(steps, out int stepsInt) || stepsInt < 0 || stepsInt > 100000)
            {
                ShowStatus("步数格式错误，请输入 0-100000 之间的数字", Brushes.Red);
                return;
            }

            // Disable button and show loading
            IsNotBusy = false;
            SubmitButton.Content = "修改中...";
            ShowStatus("正在修改步数，请稍候...", Brushes.Blue);

            try
            {
                // Call the change steps method
                string result = await Runner.ChangeStepsAsync(account, password, steps);

                // Check result
                if (result.Contains("成功"))
                {
                    ShowStatus(result, Brushes.Green);
                }
                else
                {
                    ShowStatus(result, Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"发生错误: {ex.Message}", Brushes.Red);
            }
            finally
            {
                // Re-enable button
                IsNotBusy = true;
                SubmitButton.Content = "修改步数";
            }
        }

        private void ShowStatus(string message, Brush color)
        {
            _statusMessage = message;
            StatusColor = color;
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = color;
            StatusTextBlock.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
            OnPropertyChanged(nameof(HasStatusMessage));
            OnPropertyChanged(nameof(StatusColor));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

