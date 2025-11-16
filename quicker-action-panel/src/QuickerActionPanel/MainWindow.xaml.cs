using Quicker.Common;
using QuickerActionPanel.Views;
using System.Windows;

namespace QuickerActionPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ActionDropHandler? _dropHandler;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDropHandler();
        }

        private void InitializeDropHandler()
        {
            _dropHandler = Resources["ActionDropHandler"] as ActionDropHandler;
            if (_dropHandler != null)
            {
                _dropHandler.ActionDropped += DropHandler_ActionDropped;
            }
        }

        private void DropHandler_ActionDropped(ActionItem actionItem)
        {
            try
            {
                var message = $"Action Dropped!\n\n" +
                             $"Title: {actionItem.Title ?? "N/A"}\n" +
                             $"ID: {actionItem.Id ?? "N/A"}\n" +
                             $"Description: {actionItem.Description ?? "N/A"}\n" +
                             $"Icon: {actionItem.Icon ?? "N/A"}\n" +
                             $"Template ID: {actionItem.TemplateId ?? "N/A"}";

                DropStatusText.Text = message;
            }
            catch (System.Exception ex)
            {
                var errorMessage = $"Error processing dropped action: {ex.Message}";
                DropStatusText.Text = errorMessage;
            }
        }

        private void OpenActionPanelButton_Click(object sender, RoutedEventArgs e)
        {
            var actionPanelWindow = new ActionPanelWindow();
            actionPanelWindow.Show();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (_dropHandler != null)
            {
                _dropHandler.ActionDropped -= DropHandler_ActionDropped;
            }
            base.OnClosed(e);
        }
    }
}
