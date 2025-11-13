using System;
using System.Linq;

namespace WindowEdgeHide.Models
{
    /// <summary>
    /// Represents thickness values for four sides (left, top, right, bottom)
    /// Similar to WPF Thickness, supports single value (all sides), two values (horizontal, vertical), or four values (left, top, right, bottom)
    /// </summary>
    public struct IntThickness
    {
        /// <summary>
        /// Thickness value for left side
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// Thickness value for top side
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// Thickness value for right side
        /// </summary>
        public int Right { get; set; }

        /// <summary>
        /// Thickness value for bottom side
        /// </summary>
        public int Bottom { get; set; }

        /// <summary>
        /// Initialize with single value (all sides equal)
        /// </summary>
        /// <param name="uniformValue">Value for all sides</param>
        public IntThickness(int uniformValue)
        {
            Left = Top = Right = Bottom = uniformValue;
        }

        /// <summary>
        /// Initialize with horizontal and vertical values
        /// </summary>
        /// <param name="horizontal">Value for left and right</param>
        /// <param name="vertical">Value for top and bottom</param>
        public IntThickness(int horizontal, int vertical)
        {
            Left = Right = horizontal;
            Top = Bottom = vertical;
        }

        /// <summary>
        /// Initialize with four values
        /// </summary>
        /// <param name="left">Value for left</param>
        /// <param name="top">Value for top</param>
        /// <param name="right">Value for right</param>
        /// <param name="bottom">Value for bottom</param>
        public IntThickness(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// Get horizontal thickness (max of left and right)
        /// </summary>
        public int Horizontal => Math.Max(Left, Right);

        /// <summary>
        /// Get vertical thickness (max of top and bottom)
        /// </summary>
        public int Vertical => Math.Max(Top, Bottom);

        /// <summary>
        /// Get thickness value for a specific edge direction
        /// </summary>
        public int GetValue(EdgeDirection direction)
        {
            return direction switch
            {
                EdgeDirection.Left => Left,
                EdgeDirection.Top => Top,
                EdgeDirection.Right => Right,
                EdgeDirection.Bottom => Bottom,
                _ => Horizontal
            };
        }

        /// <summary>
        /// Parses a string representation of IntThickness.
        /// Supported formats:
        /// "5" -> uniform thickness of 5 for all sides
        /// "5,6" -> horizontal thickness 5, vertical thickness 6
        /// "1,2,3,4" -> left 1, top 2, right 3, bottom 4
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>An IntThickness instance.</returns>
        /// <exception cref="FormatException">Thrown if the string format is invalid.</exception>
        public static IntThickness Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new FormatException("Input string cannot be null or empty.");
            }

            string[] parts = s.Split(',');
            int[] values = parts.Select(p => int.Parse(p.Trim())).ToArray();

            switch (values.Length)
            {
                case 1:
                    return new IntThickness(values[0]);
                case 2:
                    return new IntThickness(values[0], values[1]);
                case 4:
                    return new IntThickness(values[0], values[1], values[2], values[3]);
                default:
                    throw new FormatException("Invalid IntThickness format. Expected 'value', 'horizontal,vertical', or 'left,top,right,bottom'.");
            }
        }

        public override string ToString()
        {
            if (Left == Top && Top == Right && Right == Bottom)
            {
                return Left.ToString();
            }
            if (Left == Right && Top == Bottom)
            {
                return $"{Left},{Top}";
            }
            return $"{Left},{Top},{Right},{Bottom}";
        }

        public bool Equals(IntThickness other)
        {
            return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
        }

        public override bool Equals(object? obj)
        {
            return obj is IntThickness other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Left.GetHashCode();
                hash = hash * 23 + Top.GetHashCode();
                hash = hash * 23 + Right.GetHashCode();
                hash = hash * 23 + Bottom.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(IntThickness left, IntThickness right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IntThickness left, IntThickness right)
        {
            return !(left == right);
        }
    }
}

