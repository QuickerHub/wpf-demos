using System;
using System.Windows;

namespace QuickerActionManage.View.Editor
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PropertyGridAttribute : Attribute
    {
        public PropertyGridAttribute()
        {
        }
        public static PropertyGridAttribute Default = new PropertyGridAttribute() { Title = "属性" };
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Grouping { get; set; }
        public bool EnableSearch { get; set; }
        public void SetGrid(PropertyGridPlus grid)
        {
            grid.Grouping = Grouping;
            grid.FilterBarVisibility = EnableSearch ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

