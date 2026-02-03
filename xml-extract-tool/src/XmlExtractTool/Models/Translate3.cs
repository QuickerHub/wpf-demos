using System;
using System.Globalization;

namespace XmlExtractTool.Models
{
    /// <summary>
    /// 3D translation (x, y, z) for KeyFrame translate attribute.
    /// </summary>
    public struct Translate3(double x, double y, double z)
    {
        public double X { get; set; } = x;
        public double Y { get; set; } = y;
        public double Z { get; set; } = z;

        public static bool TryParse(string? s, out Translate3 t)
        {
            t = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Split(',');
            if (parts == null || parts.Length < 3) return false;
            if (parts[0] == null || !double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                return false;
            if (parts[1] == null || !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                return false;
            if (parts[2] == null || !double.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                return false;
            t = new Translate3(x, y, z);
            return true;
        }

        public readonly bool IsZero(double tolerance = 1e-5) =>
            Math.Abs(X) <= tolerance && Math.Abs(Y) <= tolerance && Math.Abs(Z) <= tolerance;
    }
}
