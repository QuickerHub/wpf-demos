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
        /// Window activation strategy when showing from edge hide
        /// AutoActivate: Auto-activate window when showing. For noactive windows, automatically fallback to Topmost
        /// Topmost: Set window to topmost without activation
        /// None: Do nothing (no activation, no topmost)
        /// </summary>
        public ActivationStrategy ActivationStrategy { get; set; } = ActivationStrategy.AutoActivate;

        /// <summary>
        /// Edge direction for window restore/update
        /// If None, automatically selects nearest edge based on current position
        /// </summary>
        public EdgeDirection UpdateEdgeDirection { get; set; } = EdgeDirection.None;

        /// <summary>
        /// [Legacy] Whether to automatically set window to topmost
        /// This property is kept for backward compatibility and will be converted to ActivationStrategy
        /// </summary>
        [Obsolete("Use ActivationStrategy instead. This property is kept for backward compatibility.")]
        public bool AutoTopmost { get; set; } = true;

        /// <summary>
        /// [Legacy] Whether to use focus-aware activation strategy
        /// This property is kept for backward compatibility and will be converted to ActivationStrategy
        /// </summary>
        [Obsolete("Use ActivationStrategy instead. This property is kept for backward compatibility.")]
        public bool UseFocusAwareActivation { get; set; } = false;
    }
}

