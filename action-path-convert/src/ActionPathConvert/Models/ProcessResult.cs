using System.Collections.Generic;

namespace ActionPathConvert.Models
{
    /// <summary>
    /// Result of processing a single input file
    /// </summary>
    public class ProcessResult
    {
        /// <summary>
        /// Path to the generated M3U file
        /// </summary>
        public string M3uFilePath { get; set; } = "";

        /// <summary>
        /// List of files that were not found during processing
        /// </summary>
        public List<string> NotFoundFiles { get; set; } = new List<string>();

        /// <summary>
        /// Display name for the result (file name)
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(M3uFilePath))
                {
                    return System.IO.Path.GetFileName(M3uFilePath);
                }
                return $"未找到文件 ({NotFoundFiles.Count} 个)";
            }
        }
    }
}

