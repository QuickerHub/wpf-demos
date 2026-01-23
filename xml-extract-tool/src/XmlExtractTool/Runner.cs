using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using XmlExtractTool.Services;

namespace XmlExtractTool
{
    /// <summary>
    /// Runner for Quicker interface - provides entry points for Quicker actions
    /// </summary>
    public static class Runner
    {
        private static MainWindow? _mainWindow;
        private static readonly XmlQuaternionChecker _checker = new();

        /// <summary>
        /// Show main window (UI thread only, singleton)
        /// </summary>
        public static void Run()
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
        /// Check quaternions from file path or XML text, return list of node names that don't have 90-degree rotation
        /// Automatically detects whether input is a file path or XML text content
        /// </summary>
        /// <param name="input">File path or XML text content</param>
        /// <returns>List of node names that don't satisfy 90-degree rotation condition</returns>
        public static List<string> CheckQuaternions(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            try
            {
                return _checker.CheckQuaternionsAuto(input);
            }
            catch
            {
                // Return empty list on error
                return new List<string>();
            }
        }
    }
}
