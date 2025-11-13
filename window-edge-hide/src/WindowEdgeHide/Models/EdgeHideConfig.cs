using System;
using Newtonsoft.Json;

namespace WindowEdgeHide.Models
{
    /// <summary>
    /// Configuration for edge hiding registration
    /// </summary>
    public class EdgeHideConfig
    {
        /// <summary>
        /// Window handle
        /// </summary>
        [JsonConverter(typeof(IntPtrJsonConverter))]
        public IntPtr WindowHandle { get; set; }

        /// <summary>
        /// Edge direction
        /// </summary>
        public EdgeDirection EdgeDirection { get; set; } = EdgeDirection.Nearest;

        /// <summary>
        /// Visible area thickness
        /// </summary>
        [JsonConverter(typeof(IntThicknessJsonConverter))]
        public IntThickness VisibleArea { get; set; } = new IntThickness(5);

        /// <summary>
        /// Animation type for window movement
        /// </summary>
        public AnimationType AnimationType { get; set; } = AnimationType.None;

        /// <summary>
        /// Whether to show window when mouse is at screen edge
        /// </summary>
        public bool ShowOnScreenEdge { get; set; } = false;

        /// <summary>
        /// Whether to automatically set window to topmost
        /// </summary>
        public bool AutoTopmost { get; set; } = true;
    }
}

