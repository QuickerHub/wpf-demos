using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;

namespace QuickerActionManage.View.Editor
{
    /// <summary>
    /// 排序方向编辑器，使用 RadioButton 实现升序/降序选择
    /// </summary>
    public class SortDirectionEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            var control = new SortDirectionRadioButtonControl
            {
                IsEnabled = !propertyItem.IsReadOnly,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            
            // 确保在加载后重新设置对齐，防止 PropertyItem 的布局影响
            control.Loaded += (s, e) =>
            {
                if (s is SortDirectionRadioButtonControl ctrl)
                {
                    ctrl.VerticalAlignment = VerticalAlignment.Center;
                    ctrl.HorizontalAlignment = HorizontalAlignment.Left;
                    // 确保内部 Grid 也正确对齐
                    if (ctrl.Content is Grid grid)
                    {
                        grid.VerticalAlignment = VerticalAlignment.Center;
                        grid.HorizontalAlignment = HorizontalAlignment.Left;
                    }
                }
            };
            
            return control;
        }

        public override DependencyProperty GetDependencyProperty()
        {
            return SortDirectionRadioButtonControl.SortDirectionProperty;
        }
    }
}

