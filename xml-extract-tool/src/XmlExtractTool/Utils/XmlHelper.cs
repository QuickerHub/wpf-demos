using System;
using System.IO;
using System.Xml.Linq;

namespace XmlExtractTool.Utils
{
    /// <summary>
    /// XML processing utility class
    /// </summary>
    public static class XmlHelper
    {
        /// <summary>
        /// Load XML document from file path
        /// </summary>
        public static XDocument LoadXml(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"XML file not found: {filePath}");

            try
            {
                return XDocument.Load(filePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load XML file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate XML document structure
        /// </summary>
        public static bool IsValidXml(XDocument? document)
        {
            return document != null && document.Root != null;
        }
    }
}
