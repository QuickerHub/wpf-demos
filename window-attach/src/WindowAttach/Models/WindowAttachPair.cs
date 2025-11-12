using System;

namespace WindowAttach.Models
{
    /// <summary>
    /// Represents a pair of windows that are attached
    /// </summary>
    public class WindowAttachPair
    {
        public IntPtr Window1Handle { get; set; }
        public IntPtr Window2Handle { get; set; }
        public WindowPlacement Placement { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public bool RestrictToSameScreen { get; set; }
        public DateTime RegisteredTime { get; set; } = DateTime.Now;
        public AttachType AttachType { get; set; } = AttachType.Main;

        /// <summary>
        /// Generate a unique key for this pair
        /// </summary>
        public string GetKey() => $"{Window1Handle}_{Window2Handle}_{AttachType}";
    }
}

