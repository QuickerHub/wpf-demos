using System;
using System.Windows.Media;
using System.Collections.Generic;
using HandyControl.Controls;
using System.Collections;

namespace QuickerActionManage.View.Editor
{
    /// <summary>
    /// 解决类型 Type.GetType() 找不到的问题
    /// </summary>
    public class PropertyResolverPlus : PropertyResolver
    {
        private enum EditorTypeCode
        {
            Text,
            Number,
            Brush,
            FontFamily
        };
        private readonly Dictionary<Type, EditorTypeCode> _editors = new()
        {
            [typeof(string)] = EditorTypeCode.Text,
            [typeof(int)] = EditorTypeCode.Number,
            [typeof(float)] = EditorTypeCode.Number,
            [typeof(double)] = EditorTypeCode.Number,
            [typeof(Brush)] = EditorTypeCode.Brush,
            [typeof(FontFamily)] = EditorTypeCode.FontFamily
        };

        public override PropertyEditorBase CreateDefaultEditor(Type type)
        {
            PropertyEditorBase result;
            if (_editors.TryGetValue(type, out var editor))
            {
                result = editor switch
                {
                    EditorTypeCode.Text => new TextPropertyEditor(),
                    EditorTypeCode.Number => new NumEditor(),
                    EditorTypeCode.Brush => new ColorEditor(),
                    _ => new ReadOnlyTextPropertyEditor(),
                };
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                result = new ListEditor();
            }
            else if (type.IsSubclassOf(typeof(Enum)))
            {
                result = new EnumEditor();
            }
            else
            {
                result = base.CreateDefaultEditor(type);
            }
            return result;
        }
        public override PropertyEditorBase CreateEditor(Type type)
        {
            base.CreateEditor(type);
            try
            {
                return Activator.CreateInstance(type) as PropertyEditorBase ?? new ReadOnlyTextPropertyEditor();
            }
            catch
            {
                return new ReadOnlyTextPropertyEditor();
            }
        }
    }
}

