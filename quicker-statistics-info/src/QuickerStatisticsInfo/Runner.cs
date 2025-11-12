using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using QuickerStatisticsInfo.View;

namespace QuickerStatisticsInfo
{
    /// <summary>
    /// Runner for showing main window (singleton pattern)
    /// </summary>
    public static class Runner
    {
        private static MainWindow? _mainWindow;
        private static StatisticsWindow? _statisticsWindow;

        /// <summary>
        /// Show main window (UI thread only, singleton)
        /// </summary>
        public static void ShowMainWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // If window exists and is not closed, activate it
                if (_mainWindow != null && _mainWindow.IsLoaded)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    return;
                }

                // Create new window instance
                _mainWindow = new MainWindow();
                
                // Remove reference when window is closed
                _mainWindow.Closed += (s, e) =>
                {
                    _mainWindow = null;
                };

                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Show();
                _mainWindow.Activate();
            });
        }

        /// <summary>
        /// Show statistics window and start collecting statistics from URL
        /// </summary>
        /// <param name="url">URL like "https://getquicker.net/User/Actions/113342-Cea" or "https://getquicker.net/Sharedaction?code=..."</param>
        public static void ShowStatisticsWindow(string url)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Create and show window
                CreateAndShowStatisticsWindow("正在解析URL...");

                // Extract user path and start collecting asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        string userPath = await ExtractUserPathFromUrlAsync(url);
                        StartStatisticsCollection(userPath);
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"无法从URL中提取用户路径：{ex.Message}\n\nURL: {url}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            _statisticsWindow?.Close();
                        });
                    }
                });
            });
        }

        /// <summary>
        /// Create and show statistics window
        /// </summary>
        /// <param name="initialStatus">Initial status text</param>
        private static void CreateAndShowStatisticsWindow(string initialStatus)
        {
            // If window exists and is not closed, close it first
            if (_statisticsWindow != null && _statisticsWindow.IsLoaded)
            {
                _statisticsWindow.Close();
            }

            // Create new statistics window
            _statisticsWindow = new StatisticsWindow();
            _statisticsWindow.ViewModel.StatusText = initialStatus;
            
            // Remove reference when window is closed
            _statisticsWindow.Closed += (s, e) =>
            {
                _statisticsWindow = null;
            };

            // Show window
            _statisticsWindow.WindowState = WindowState.Normal;
            _statisticsWindow.Show();
            _statisticsWindow.Activate();
            _statisticsWindow.UpdateLayout();
        }

        /// <summary>
        /// Start statistics collection with user path
        /// </summary>
        /// <param name="userPath">User path like "113342-Cea"</param>
        private static void StartStatisticsCollection(string userPath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_statisticsWindow != null && _statisticsWindow.IsLoaded)
                {
                    _statisticsWindow.Initialize(userPath);
                    _statisticsWindow.StartCollecting(userPath);
                }
            });
        }

        /// <summary>
        /// Extract user path from URL (supports both user page and action page URLs)
        /// </summary>
        /// <param name="url">URL like "https://getquicker.net/User/Actions/113342-Cea" or "https://getquicker.net/Sharedaction?code=..."</param>
        /// <returns>User path like "113342-Cea"</returns>
        /// <exception cref="ArgumentException">Thrown when unable to extract user path from URL</exception>
        private static async Task<string> ExtractUserPathFromUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL不能为空", nameof(url));

            // Pattern 1: Direct user page URL - /User/Actions/{userPath}
            var match = Regex.Match(url, @"/User/Actions/([^/?]+)");
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            // Pattern 2: Action page URL - /Sharedaction?code=...
            if (url.Contains("/Sharedaction") || url.Contains("Sharedaction"))
            {
                // Extract action URL path
                string actionUrl = url;
                if (actionUrl.StartsWith("http://") || actionUrl.StartsWith("https://"))
                {
                    var uri = new Uri(actionUrl);
                    actionUrl = uri.PathAndQuery;
                }

                // Extract user path from action page
                using (var collector = new QuickerStatisticsCollector())
                {
                    string userPath = await collector.ExtractUserPathFromActionPageAsync(actionUrl);
                    if (!string.IsNullOrEmpty(userPath))
                    {
                        return userPath;
                    }
                }
            }

            // If we reach here, we couldn't extract the user path
            throw new ArgumentException($"无法从URL中提取用户路径：{url}", nameof(url));
        }
    }
}

