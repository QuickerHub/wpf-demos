using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BatchRenameTool.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BatchRenameTool.ViewModels
{
    /// <summary>
    /// ViewModel for batch rename tool
    /// </summary>
    public partial class BatchRenameViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial ObservableCollection<FileRenameItem> Items { get; set; } = new();

        [ObservableProperty]
        public partial string RenamePattern { get; set; } = "";

        /// <summary>
        /// Available completion items for variable completion
        /// </summary>
        public IEnumerable<string> CompletionItems { get; } = new[] { "name", "ext", "fullname" };

        /// <summary>
        /// Add files to rename list
        /// </summary>
        [RelayCommand]
        private void AddFiles(string? folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            var files = Directory.GetFiles(folderPath);
            
            // Sort files using natural sort order
            var comparer = new NaturalStringComparer();
            var sortedFiles = files.OrderBy(Path.GetFileName, comparer).ToArray();

            foreach (var file in sortedFiles)
            {
                var fileName = Path.GetFileName(file);
                var fileExtension = Path.GetExtension(file);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                // Generate new name (for now, just add prefix as example)
                var newName = $"New_{fileNameWithoutExtension}{fileExtension}";

                Items.Add(new FileRenameItem
                {
                    OriginalName = fileName,
                    NewName = newName,
                    FullPath = file
                });
            }

            UpdateRenamePreview();
        }

        /// <summary>
        /// Clear all items
        /// </summary>
        [RelayCommand]
        private void ClearItems()
        {
            Items.Clear();
        }

        /// <summary>
        /// Update rename preview based on pattern
        /// </summary>
        partial void OnRenamePatternChanged(string value)
        {
            UpdateRenamePreview();
        }

        /// <summary>
        /// Update preview of renamed files
        /// </summary>
        private void UpdateRenamePreview()
        {
            if (string.IsNullOrWhiteSpace(RenamePattern))
            {
                // Default: add prefix
                foreach (var item in Items)
                {
                    var extension = Path.GetExtension(item.OriginalName);
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(item.OriginalName);
                    item.NewName = $"New_{nameWithoutExt}{extension}";
                }
            }
            else
            {
                // Apply rename pattern
                // For now, simple replacement: {name} will be replaced with original name
                foreach (var item in Items)
                {
                    var extension = Path.GetExtension(item.OriginalName);
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(item.OriginalName);
                    var newName = RenamePattern
                        .Replace("{name}", nameWithoutExt)
                        .Replace("{ext}", extension.TrimStart('.'))
                        .Replace("{fullname}", item.OriginalName);

                    // Ensure extension is preserved if not in pattern
                    if (!newName.Contains(".") && !string.IsNullOrEmpty(extension))
                    {
                        newName += extension;
                    }

                    item.NewName = newName;
                }
            }
        }

        /// <summary>
        /// Execute rename operation
        /// </summary>
        [RelayCommand]
        private void ExecuteRename()
        {
            if (Items.Count == 0)
                return;

            var renamedItems = new System.Collections.Generic.List<FileRenameItem>();

            foreach (var item in Items.ToList())
            {
                try
                {
                    var directory = Path.GetDirectoryName(item.FullPath);
                    if (directory == null)
                        continue;

                    var newFullPath = Path.Combine(directory, item.NewName);
                    
                    // Skip if new name is same as original
                    if (newFullPath == item.FullPath)
                        continue;

                    if (File.Exists(newFullPath))
                    {
                        // File already exists, skip
                        continue;
                    }

                    File.Move(item.FullPath, newFullPath);
                    item.FullPath = newFullPath;
                    item.OriginalName = item.NewName;
                    renamedItems.Add(item);
                }
                catch
                {
                    // Handle error (could add error logging here)
                }
            }

            // Refresh the list after rename
            UpdateRenamePreview();
        }
    }

    /// <summary>
    /// Represents a file item for renaming
    /// </summary>
    public partial class FileRenameItem : ObservableObject
    {
        [ObservableProperty]
        public partial string OriginalName { get; set; } = "";

        [ObservableProperty]
        public partial string NewName { get; set; } = "";

        [ObservableProperty]
        public partial string FullPath { get; set; } = "";

        /// <summary>
        /// Check if the name has been changed
        /// </summary>
        public bool IsNameChanged => OriginalName != NewName;

        partial void OnNewNameChanged(string value)
        {
            OnPropertyChanged(nameof(IsNameChanged));
        }

        partial void OnOriginalNameChanged(string value)
        {
            OnPropertyChanged(nameof(IsNameChanged));
        }
    }
}
