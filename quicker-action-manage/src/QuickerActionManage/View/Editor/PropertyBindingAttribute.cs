using System;
using System.Linq;
using System.Windows;
using System.Reflection;
using System.Collections;
using System.Windows.Data;

namespace QuickerActionManage.View.Editor
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PropertyBindingAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">获取 ItemsSource 的时候会用到</param>
        public PropertyBindingAttribute(Type type)
        {
            _sourceType = type;
        }

        private readonly Type _sourceType;
        public Type SourceType => _sourceType;
        public string IsEnableName { get; set; }
        public void SetIsEnableBinding(FrameworkElement element, object source)
        {
            SetBinding(element, UIElement.IsEnabledProperty, source, IsEnableName);
        }

        public string ItemsSourceName { get; set; }

        public IEnumerable? GetItemsSource()
        {
            var mem = SourceType
                .GetMember(ItemsSourceName, BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault();
            if (mem is PropertyInfo prop)
            {
                return (IEnumerable)prop.GetValue(null);
            }
            else if (mem is FieldInfo field)
            {
                return (IEnumerable)field.GetValue(null);
            }
            return null;
        }

        public void SetBinding(FrameworkElement element, DependencyProperty prop, object source, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            element.SetBinding(prop, new Binding(path) { Source = source });
        }
    }
}

