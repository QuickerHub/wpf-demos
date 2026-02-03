using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using XmlExtractTool.Models;
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
        private static readonly CheckerSettings _settings = new();
        private static readonly XmlNodeChecker _nodeChecker = new XmlNodeChecker(_settings);

        static Runner()
        {
            _settings.Load();
        }

        /// <summary>
        /// Show main window (UI thread only, singleton)
        /// </summary>
        public static void Run()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_mainWindow != null && _mainWindow.IsLoaded)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    return;
                }

                _mainWindow = new MainWindow();
                _mainWindow.Closed += (s, e) => { _mainWindow = null; };
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Show();
                _mainWindow.Activate();
            });
        }

        /// <summary>
        /// Check from file path, folder path, or XML text. Returns list of result strings (format: FileName\nNodeName\nParent per item).
        /// </summary>
        /// <param name="input">File path, folder path, or XML text content</param>
        /// <param name="showUI">If true, show MainWindow and display results; if false, return results directly</param>
        /// <returns>List of result strings (empty if showUI is true)</returns>
        public static List<string> CheckQuaternions(string input, bool showUI = false)
        {
            if (string.IsNullOrWhiteSpace(input)) return [];

            if (showUI)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_mainWindow == null || !_mainWindow.IsLoaded)
                    {
                        _mainWindow = new MainWindow();
                        _mainWindow.Closed += (s, e) => { _mainWindow = null; };
                    }
                    _mainWindow.WindowState = WindowState.Normal;
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    if (Directory.Exists(input))
                        _mainWindow.ViewModel.LoadFolderAndCheck(input);
                    else
                        _mainWindow.LoadInput(input);
                });
                return [];
            }

            try
            {
                if (Directory.Exists(input))
                {
                    var items = _nodeChecker.CheckFolder(input);
                    return items.Select(i => i.ToString()).ToList();
                }
                var fileItems = _nodeChecker.CheckFile(input);
                if (fileItems.Count > 0)
                    return fileItems.Select(i => i.ToString()).ToList();
                return _checker.CheckQuaternionsAuto(input);
            }
            catch
            {
                return [];
            }
        }
    }
}
