using System;

namespace QuickerActionManage.View.Editor
{
    public class EnumEditorAttribute : Attribute
    {
        public bool AllMultiple { get; set; }
        public EnumEditorAttribute(bool allMultiple)
        {
            AllMultiple = allMultiple;
        }
    }
}

