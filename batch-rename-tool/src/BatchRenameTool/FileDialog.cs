using System;
using System.IO;
using System.Windows.Forms;

namespace BatchRenameTool
{
    /// <summary>
    /// File and folder selection dialogs using WinForms
    /// </summary>
    public static class FileDialog
    {
        /// <summary>
        /// Show open file dialog for selecting multiple files
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="filter">File filter</param>
        /// <param name="filterIndex">Default filter index</param>
        /// <returns>Selected file paths, or null if cancelled</returns>
        public static string[]? ShowOpenFileDialog(string title = "选择文件", string filter = "所有文件 (*.*)|*.*", int filterIndex = 1)
        {
            try
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Multiselect = true;
                    dialog.Filter = filter;
                    dialog.FilterIndex = filterIndex;
                    dialog.Title = title;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        return dialog.FileNames;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileDialog.ShowOpenFileDialog error: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Show folder browser dialog for selecting a directory
        /// </summary>
        /// <param name="description">Dialog description</param>
        /// <param name="selectedPath">Initial selected path</param>
        /// <param name="showNewFolderButton">Whether to show new folder button</param>
        /// <returns>Selected folder path, or null if cancelled</returns>
        public static string? ShowFolderBrowserDialog(string description = "选择文件夹", string? selectedPath = null, bool showNewFolderButton = true)
        {
            try
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = description;
                    dialog.ShowNewFolderButton = showNewFolderButton;
                    
                    if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
                    {
                        dialog.SelectedPath = selectedPath;
                    }

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        return dialog.SelectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileDialog.ShowFolderBrowserDialog error: {ex.Message}");
            }

            return null;
        }
    }
}
