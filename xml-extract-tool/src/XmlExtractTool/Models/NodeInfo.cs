namespace XmlExtractTool.Models
{
    /// <summary>
    /// Node information model containing name and quaternion rotation
    /// </summary>
    public class NodeInfo
    {
        /// <summary>
        /// Node name attribute
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Quaternion rotation value from KeyFrame rotate attribute
        /// </summary>
        public Quaternion? Quaternion { get; set; }

        /// <summary>
        /// Whether this node satisfies the 90-degree rotation condition
        /// </summary>
        public bool Is90DegreeRotation { get; set; }

        /// <summary>
        /// Raw rotate attribute string value
        /// </summary>
        public string? RotateString { get; set; }
    }
}
