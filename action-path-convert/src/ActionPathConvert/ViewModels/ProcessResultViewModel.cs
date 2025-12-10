using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ActionPathConvert.ViewModels
{
    /// <summary>
    /// ViewModel for a single process result
    /// </summary>
    public partial class ProcessResultViewModel : ObservableObject
    {
        /// <summary>
        /// Path to the generated M3U file
        /// </summary>
        [ObservableProperty]
        private string _m3uFilePath = "";

        /// <summary>
        /// List of files that were not found during processing
        /// </summary>
        [ObservableProperty]
        private List<string> _notFoundFiles = new List<string>();

        /// <summary>
        /// Number of successfully matched files
        /// </summary>
        [ObservableProperty]
        private int _matchedFilesCount = 0;

        /// <summary>
        /// Input file name (for display when no M3U file is generated)
        /// </summary>
        [ObservableProperty]
        private string _inputFileName = "";

        /// <summary>
        /// Merged text of not found files (one per line)
        /// </summary>
        public string NotFoundFilesText => string.Join("\r\n", NotFoundFiles);

        /// <summary>
        /// Display name for the result with detailed summary
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(M3uFilePath))
                {
                    var fileName = System.IO.Path.GetFileName(M3uFilePath);
                    var parts = new List<string>();
                    
                    if (MatchedFilesCount > 0)
                    {
                        parts.Add($"匹配 {MatchedFilesCount} 个");
                    }
                    
                    if (NotFoundFiles.Count > 0)
                    {
                        parts.Add($"未找到 {NotFoundFiles.Count} 个");
                    }
                    
                    if (parts.Count > 0)
                    {
                        return $"{fileName} ({string.Join(", ", parts)})";
                    }
                    
                    return fileName;
                }
                
                // No M3U file generated
                var inputName = !string.IsNullOrEmpty(InputFileName) 
                    ? System.IO.Path.GetFileName(InputFileName) 
                    : "输入文件";
                return $"{inputName} (未找到 {NotFoundFiles.Count} 个文件)";
            }
        }

        /// <summary>
        /// Update NotFoundFiles and notify property change for NotFoundFilesText
        /// </summary>
        public void UpdateNotFoundFiles(List<string> notFoundFiles)
        {
            NotFoundFiles = notFoundFiles ?? new List<string>();
            OnPropertyChanged(nameof(NotFoundFiles));
            OnPropertyChanged(nameof(NotFoundFilesText));
            OnPropertyChanged(nameof(DisplayName));
        }

        partial void OnNotFoundFilesChanged(List<string> value)
        {
            OnPropertyChanged(nameof(NotFoundFilesText));
            OnPropertyChanged(nameof(DisplayName));
        }

        partial void OnMatchedFilesCountChanged(int value)
        {
            OnPropertyChanged(nameof(DisplayName));
        }

        partial void OnInputFileNameChanged(string value)
        {
            OnPropertyChanged(nameof(DisplayName));
        }
    }
}

