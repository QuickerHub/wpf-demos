using System;
using System.Globalization;

namespace XmlExtractTool.Models
{
    /// <summary>
    /// Quaternion model and utility class for rotation detection
    /// </summary>
    public struct Quaternion(double x, double y, double z, double w)
    {
        public double X { get; set; } = x;
        public double Y { get; set; } = y;
        public double Z { get; set; } = z;
        public double W { get; set; } = w;

        /// <summary>
        /// Parse quaternion from string format "x,y,z,w"
        /// </summary>
        public static bool TryParse(string? quaternionString, out Quaternion quaternion)
        {
            quaternion = default;

            if (string.IsNullOrWhiteSpace(quaternionString))
                return false;

            var parts = quaternionString.Split(',');
            if (parts == null || parts.Length != 4)
                return false;

            if (parts[0] == null || !double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                return false;
            if (parts[1] == null || !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                return false;
            if (parts[2] == null || !double.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                return false;
            if (parts[3] == null || !double.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double w))
                return false;

            quaternion = new Quaternion(x, y, z, w);
            return true;
        }

        /// <summary>
        /// Get the rotation angle in degrees represented by this quaternion
        /// For a unit quaternion q = (x, y, z, w), the rotation angle θ = 2 * arccos(|w|)
        /// </summary>
        public readonly double GetRotationAngleDegrees()
        {
            // Normalize the quaternion first
            var magnitude = Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
            if (magnitude < 1e-10)
                return double.NaN; // Invalid quaternion

            var normalizedW = Math.Abs(W / magnitude);
            
            // Clamp w to [-1, 1] to avoid domain errors in arccos
            normalizedW = Math.Max(-1.0, Math.Min(1.0, normalizedW));
            
            // Calculate rotation angle: θ = 2 * arccos(|w|)
            // Convert from radians to degrees
            var angleRadians = 2.0 * Math.Acos(normalizedW);
            var angleDegrees = angleRadians * 180.0 / Math.PI;
            
            return angleDegrees;
        }

        /// <summary>
        /// Check if this quaternion represents a 90-degree rotation
        /// Calculates the actual rotation angle and checks if it's approximately 90°
        /// Note: 0 degrees (no rotation) is NOT considered as 90-degree rotation
        /// </summary>
        public readonly bool Is90DegreeRotation(double angleEpsilon = 1.0)
        {
            var angle = GetRotationAngleDegrees();
            
            if (double.IsNaN(angle))
                return false; // Invalid quaternion

            // Only check if angle is approximately 90 degrees
            // 0 degrees (no rotation) is NOT considered as 90-degree rotation
            return Math.Abs(angle - 90.0) < angleEpsilon;
        }

        public override readonly string ToString() => $"{X},{Y},{Z},{W}";
    }
}
