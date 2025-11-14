using System;

namespace QuickerActionManage.View.Editor
{
    [AttributeUsage(AttributeTargets.All)]
    public class NumEditorPropertyAttribute : Attribute
    {
        public NumEditorPropertyAttribute(double min, double max, double increment = 1)
        {
            Minimum = min;
            Maximum = max;
            Increment = increment;
        }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Increment { get; set; }
        public void SetToNumericUpDown(HandyControl.Controls.NumericUpDown control)
        {
            control.Minimum = Minimum;
            control.Maximum = Maximum;
            control.Increment = Increment;
        }
    }
}

