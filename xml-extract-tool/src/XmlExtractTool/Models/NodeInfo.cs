namespace XmlExtractTool.Models
{
    /// <summary>
    /// Node information model containing name, nodeType, parent, quaternion and translate.
    /// </summary>
    public class NodeInfo
    {
        public string Name { get; set; } = string.Empty;
        public string NodeType { get; set; } = string.Empty;
        public string Parent { get; set; } = string.Empty;
        public Quaternion? Quaternion { get; set; }
        public Translate3? Translate { get; set; }
        public bool Is90DegreeRotation { get; set; }
        public string? RotateString { get; set; }
    }

    /// <summary>
    /// Single check result line: 文件名, Node Name, Parent (for display).
    /// </summary>
    public class CheckResultItem
    {
        public string FileName { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public string Parent { get; set; } = string.Empty;

        public override string ToString() =>
            string.IsNullOrEmpty(FileName) ? NodeName : $"{FileName}\n{NodeName}\n{Parent}";
    }
}
