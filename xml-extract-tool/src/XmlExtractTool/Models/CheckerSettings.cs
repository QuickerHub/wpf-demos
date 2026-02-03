using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XmlExtractTool.Models
{
    /// <summary>
    /// Settings for XML node checker: file extensions and LoopMode keywords.
    /// </summary>
    public partial class CheckerSettings : ObservableObject
    {
        /// <summary>读取文件格式 - one extension per line, e.g. .mil, .upe, .xml, .uff</summary>
        [ObservableProperty]
        private string _fileExtensions = ".mil\n.upe\n.xml\n.uff";

        /// <summary>LoopMode=2 不循环 - one keyword pattern per line, * = any chars</summary>
        [ObservableProperty]
        private string _keywordsLoopMode2 = "_love_3didle\n_bs\n_idle\n_gk";

        /// <summary>LoopMode=1 循环 - one keyword pattern per line</summary>
        [ObservableProperty]
        private string _keywordsLoopMode1 = "2065*_hand\n2012*.upe\n2040*.upe\n_eff\n_qianjin\n_qifei\n_xiajiang";

        public string[] GetFileExtensionList()
        {
            if (string.IsNullOrWhiteSpace(FileExtensions)) return [".mil", ".upe", ".xml", ".uff"];
            return FileExtensions.Split('\n', '\r')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }

        public string[] GetKeywordsLoopMode2()
        {
            return SplitLines(KeywordsLoopMode2);
        }

        public string[] GetKeywordsLoopMode1()
        {
            return SplitLines(KeywordsLoopMode1);
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return [];
            return text.Split('\n', '\r').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        /// <summary>
        /// Match filename against a pattern; * means any characters.
        /// </summary>
        public static bool FileNameMatchesPattern(string fileName, string pattern)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(pattern))
                return false;
            var parts = pattern.Split('*');
            if (parts.Length == 1)
                return fileName.Contains(parts[0], StringComparison.OrdinalIgnoreCase);
            int index = 0;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                int i = fileName.IndexOf(part, index, StringComparison.OrdinalIgnoreCase);
                if (i < 0) return false;
                index = i + part.Length;
            }
            return true;
        }

        public static bool MatchesAnyPattern(string fileName, string[] patterns)
        {
            foreach (var p in patterns)
            {
                if (FileNameMatchesPattern(fileName, p)) return true;
            }
            return false;
        }

        private static string GetSettingsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XmlExtractTool");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.txt");
        }

        private const string SectionFileExtensions = "[FileExtensions]";
        private const string SectionLoopMode2 = "[LoopMode2]";
        private const string SectionLoopMode1 = "[LoopMode1]";

        public void Save()
        {
            try
            {
                var path = GetSettingsPath();
                var content = SectionFileExtensions + "\n" + (FileExtensions ?? "") + "\n" +
                              SectionLoopMode2 + "\n" + (KeywordsLoopMode2 ?? "") + "\n" +
                              SectionLoopMode1 + "\n" + (KeywordsLoopMode1 ?? "");
                File.WriteAllText(path, content);
            }
            catch { /* ignore */ }
        }

        public void Load()
        {
            try
            {
                var path = GetSettingsPath();
                if (!File.Exists(path)) return;
                var lines = File.ReadAllLines(path);
                string? current = null;
                var ext = new List<string>();
                var lm2 = new List<string>();
                var lm1 = new List<string>();
                foreach (var line in lines)
                {
                    if (line.Trim() == SectionFileExtensions) { current = SectionFileExtensions; continue; }
                    if (line.Trim() == SectionLoopMode2) { current = SectionLoopMode2; continue; }
                    if (line.Trim() == SectionLoopMode1) { current = SectionLoopMode1; continue; }
                    if (current == SectionFileExtensions) ext.Add(line);
                    else if (current == SectionLoopMode2) lm2.Add(line);
                    else if (current == SectionLoopMode1) lm1.Add(line);
                }
                if (ext.Count > 0) FileExtensions = string.Join("\n", ext);
                if (lm2.Count > 0) KeywordsLoopMode2 = string.Join("\n", lm2);
                if (lm1.Count > 0) KeywordsLoopMode1 = string.Join("\n", lm1);
            }
            catch { /* ignore */ }
        }
    }
}
