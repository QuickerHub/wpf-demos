using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CoDetectNet;

namespace CoDetectNet.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _throttleTimer;
        private const int ThrottleDelayMs = 500; // Wait 500ms after user stops typing
        private static readonly Lazy<CoDetectModel> _modelLazy = new Lazy<CoDetectModel>(CreateModel);

        public MainWindow()
        {
            InitializeComponent();
            InitializeThrottleTimer();
        }

        private static CoDetectModel CreateModel()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? AppDomain.CurrentDomain.BaseDirectory;
            var modelPath = Path.Combine(assemblyDir, "codetect.onnx");
            var langPath = Path.Combine(assemblyDir, "languages.json");
            return new CoDetectModel(modelPath, langPath);
        }

        private static CoDetectModel GetModel() => _modelLazy.Value;

        private void InitializeThrottleTimer()
        {
            _throttleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ThrottleDelayMs)
            };
            _throttleTimer.Tick += ThrottleTimer_Tick;
        }

        private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Reset the throttle timer whenever text changes
            _throttleTimer?.Stop();
            
            // Hide results while user is typing
            ResultPanel.Visibility = Visibility.Collapsed;

            // Start the throttle timer
            if (!string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                _throttleTimer?.Start();
            }
            else
            {
                // If text is empty, hide results immediately
                ResultPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void ThrottleTimer_Tick(object? sender, EventArgs e)
        {
            // Stop the timer to prevent multiple triggers
            _throttleTimer?.Stop();

            // Get the current text
            var code = CodeTextBox.Text;
            
            if (string.IsNullOrWhiteSpace(code))
            {
                ResultPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // Perform language detection asynchronously
            await DetectLanguageAsync(code);
        }

        private async Task DetectLanguageAsync(string code)
        {
            try
            {
                // Run detection in background thread
                var results = await Task.Run(() =>
                {
                    var model = GetModel();
                    return model.Predict(code).Take(5).ToList();
                });

                // Update UI on the main thread
                Dispatcher.Invoke(() =>
                {
                    UpdateLanguageResults(results);
                });
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                Dispatcher.Invoke(() =>
                {
                    ResultPanel.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Error detecting language: {ex.Message}",
                                  "Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                });
            }
        }

        private void UpdateLanguageResults(List<(string Language, float Probability)> results)
        {
            if (results == null || results.Count == 0)
            {
                ResultPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // Format results as strings: "Language: 95.2%"
            var formattedResults = results.Select(r => 
                $"{r.Language}: {r.Probability * 100:F1}%").ToList();

            LanguageResults.ItemsSource = formattedResults;
            ResultPanel.Visibility = Visibility.Visible;
        }

        protected override void OnClosed(EventArgs e)
        {
            _throttleTimer?.Stop();
            _throttleTimer = null;
            base.OnClosed(e);
        }
    }
}

