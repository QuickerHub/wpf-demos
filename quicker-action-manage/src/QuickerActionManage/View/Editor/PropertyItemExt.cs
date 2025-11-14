using System;
using System.Collections.Generic;
using System.Linq;
using HandyControl.Controls;
using System.Reflection;

namespace QuickerActionManage.View.Editor
{
    public static class PropertyItemExt
    {
        /// <summary>
        /// 通过放射获取，直接用 Attributes 里面的会去除重复，就不行
        /// </summary>
        public static IEnumerable<T> GetAttributes<T>(this PropertyItem propertyItem) where T : Attribute
        {
            return propertyItem.GetPropertyInfo().GetCustomAttributes<T>();
        }
        public static T GetAttribute<T>(this PropertyItem propertyItem) where T : Attribute
        {
            return propertyItem.PropertyDescriptor.Attributes.OfType<T>().FirstOrDefault();
        }
        public static PropertyInfo GetPropertyInfo(this PropertyItem propertyItem)
        {
            var des = propertyItem.PropertyDescriptor;
            return des.ComponentType.GetProperty(des.Name);
        }
    }
}

