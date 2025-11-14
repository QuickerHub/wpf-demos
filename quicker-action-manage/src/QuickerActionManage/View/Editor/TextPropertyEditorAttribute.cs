using System;
using System.Windows;
using System.Windows.Controls;

namespace QuickerActionManage.View.Editor
{
    /// <summary>
    /// 文本编辑器
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class TextPropertyEditorAttribute : Attribute
    {
        public static TextPropertyEditorAttribute Default = new();
        public int MinLines { get; set; } = 1;
        public bool MultiLines { get; set; }
        public int MaxLines { get; set; } = int.MaxValue;
        public TextPropertyEditorAttribute()
        {
        }
        public void SetToTextBox(TextBox box)
        {
            if (MultiLines)
            {
                box.AcceptsReturn = true;
                box.VerticalContentAlignment = VerticalAlignment.Top;
            }
            box.MinLines = Math.Max(1, MinLines);
            box.MaxLines = Math.Max(MaxLines, box.MinLines);
        }
    }
}

