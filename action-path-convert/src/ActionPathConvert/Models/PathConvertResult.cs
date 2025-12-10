using System.Collections.Generic;

namespace ActionPathConvert.Models
{
    /// <summary>
    /// Path conversion result
    /// </summary>
    public class PathConvertResult
    {
        /// <summary>
        /// Successfully matched file paths
        /// </summary>
        public List<string> OutputFiles { get; set; } = new List<string>();

        /// <summary>
        /// Files not found in target directory
        /// </summary>
        public List<string> NotFoundFiles { get; set; } = new List<string>();
    }
}

