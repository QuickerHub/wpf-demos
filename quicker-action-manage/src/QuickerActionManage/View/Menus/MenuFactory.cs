using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Quicker.Public.Entities;
using Quicker.View.Controls;
using QuickerActionManage.Utils;
using QuickerActionManage.Utils.Extension;

namespace QuickerActionManage.View.Menus
{
    public static class MenuFactory
    {
        public static IconControl? GetIcon(string? icon, int size = CONST_ICON_SIZE)
        {
            if (string.IsNullOrEmpty(icon))
            {
                return null;
            }
            else
            {
                var control = new IconControl() { Icon = icon };
                if (size > 0)
                {
                    control.Width = size;
                    control.Height = size;
                }
                return control;
            }
        }

        public static string? GetDes(string? des) => string.IsNullOrEmpty(des) ? null : des;
        public static Separator GetSeparator() => new();

        public static MenuItem CreateMenuItem(string icon, string title, RoutedEventHandler? handler = null, Action<MenuItem>? action = null, IEnumerable<Control>? children = null)
        {
            return CreateMenuItem(icon, title, null, handler, action, children);
        }

        public static MenuItem CreateMenuItem(string icon, string title, string? des, RoutedEventHandler? handler = null, Action<MenuItem>? action = null, IEnumerable<Control>? children = null)
        {
            var mi = InternalCreateMenuItem(icon, title, des);
            action?.Invoke(mi);
            if (handler != null)
            {
                mi.Click += handler;
            }
            if (children != null)
            {
                AddChildMenu(mi, children);
            }
            return mi;
        }
        private static MenuItem InternalCreateMenuItem(string icon, string title, string? des)
        {
            return new MenuItem
            {
                Header = title,
                ToolTip = GetDes(des),
                Icon = GetIcon(icon)
            };
        }

        public static MenuItem CreateMenuItemWithChild(string icon, string title, Func<IEnumerable<Control>> children, Action<MenuItem>? action = null) => CreateMenuItemWithChild(icon, title, null, children(), action);

        public static MenuItem CreateMenuItemWithChild(string icon, string title, string? des, IEnumerable<Control> children, Action<MenuItem>? action = null)
        {
            var mi = InternalCreateMenuItem(icon, title, des);
            AddChildMenu(mi, children);
            action?.Invoke(mi);
            return mi;
        }

        public static void ResetChildMenu(this ItemsControl itemsControl, IEnumerable<Control> children)
        {
            itemsControl.Items.Clear();
            AddChildMenu(itemsControl, children);
        }
        public static void AddChildMenu(this ItemsControl itemsControl, IEnumerable<Control> children)
        {
            foreach (var child in children)
            {
                itemsControl.Items.Add(child);
            }
        }

        public static void AddChildByCommonOpItem(this ItemsControl itemsControl, IEnumerable<CommonOperationItem> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                if (item.IsSeparator)
                {
                    itemsControl.Items.Add(new Separator());
                }
                else
                {
                    var mi = InternalCreateMenuItem(item.Icon, item.Title, item.Description);
                    AddChildByCommonOpItem(mi, item.Children);
                    itemsControl.Items.Add(mi);
                }
            }
        }

        /// <summary>
        /// 在末尾加上分割线
        /// </summary>
        public static IEnumerable<Control> AppendSeperator(this IEnumerable<Control> menus)
        {
            object last = null;
            foreach (var item in menus)
            {
                last = item;
                yield return item;
            }
            if (last is not null and not Separator)
            {
                yield return new Separator();
            }
        }

        #region for checkbox
        public static MenuItem CreateCheckMenu(string title, string? des, object source, string path)
        {
            var mi = new MenuItem()
            {
                Header = title,
                ToolTip = GetDes(des),
                IsCheckable = true,
            };
            var binding = new Binding(path)
            {
                Source = source,
                Mode = BindingMode.TwoWay,
            };
            mi.SetBinding(MenuItem.IsCheckedProperty, binding);
            return mi;
        }
        public static MenuItem CreateCheckMenu(object source, string path)
        {
            var prop = source.GetType().GetProperty(path);
            var name = prop?.GetDisplayName();
            var des = prop?.GetDescription();
            return CreateCheckMenu(name ?? "", des, source, path);
        }
        #endregion

        public static IEnumerable<Control> GetActionContextMenu(string actionId, string? cmdata = null)
        {
            if (cmdata == null && QuickerUtil.CheckIsInQuicker())
            {
                cmdata = QuickerUtil.GetActionById(actionId)?.ContextMenuData;
            }

            if (cmdata != null)
            {
                var items = CommonOperationItem.ParseLinesWithSubItems(cmdata, true);
                foreach (var item in items)
                {
                    yield return getContextMenu(item);
                }
            }

            Control getContextMenu(CommonOperationItem item)
            {
                if (item.IsSeparator)
                {
                    return new Separator();
                }
                else
                {
                    return CreateMenuItem(item.Icon, item.Title, item.Description, (s, e) =>
                    {
                        QuickerUtil.RunActionAndRecord(actionId, item.Data);
                    }, m =>
                    {
                        if (item.Children != null)
                        {
                            AddChildMenu(m, item.Children.Select(x => getContextMenu(x)));
                        }
                    });
                }
            }
        }

        public const int CONST_ICON_SIZE = 16;

        #region static icon
        public const string COLOR_BLUE = "#FF3AA7E0";
        public const string ICON_COPY = "fa:Light_Copy";
        public const string ICON_PASTE = "fa:Light_Paste";
        public const string ICON_EDIT_BOX = "fa:Light_Edit";
        public const string ICON_DELETE = "fa:Light_Times:red";
        public const string ICON_RESTORE = $"fa:Light_TrashRestoreAlt:blue";
        public const string ICON_PLUS = "fa:Light_Plus";
        public const string ICON_LINK = "fa:Light_ExternalLinkAlt";
        public const string ICON_SHARE = "fa:Light_ShareAlt";
        public const string ICON_DEFAULT = "fa:Light_Star";
        public const string ICON_STAR = "fa:Light_Star";
        public const string ICON_RECT_WIDE = "fa:Light_RectangleWide";
        public static readonly string ICON_EDIT_PEN = $"fa:Light_Pen:{COLOR_BLUE}";
        public const string ICON_BOLT = "fa:Light_Bolt";
        public const string ICON_EMPTY = "fa:Light_EmptySet";
        public const string ICON_PLANE = "fa:Light_PaperPlane";
        public const string ICON_EXPERIMENT = "fa:Light_Flask";
        public const string ICON_PANEL = "https://files.getquicker.net/_icons/002098E8563D3F0D68BEC42AC4956CE56DD78B57.svg";
        public const string ICON_CLIP = "https://files.getquicker.net/_icons/D238156FA240C2989A8581FA7D7B6F9558552F44.png";
        public const string ICON_FAVORITE = "https://files.getquicker.net/_icons/1C6A767A0B31AFA873635A24F622464DD7FF04DD.svg";
        public const string ICON_QKSTEP = "https://files.getquicker.net/_icons/96CF03917D598EE0ECF76C7FCDFD862FA22F4E1A.svg";
        public const string ICON_QQ = "https://files.getquicker.net/_icons/DE2098DC0EF1C6F8B406BCB01830CD7511CA0AB6.png";
        public const string ICON_IMAGE = "https://files.getquicker.net/_icons/DC94325DEAD76E0F2BD6B597BB7C0D58DF83002A.svg";
        public const string ICON_TEXT = "https://files.getquicker.net/_icons/0B714B5A7755697541FFCDC82CEC6B60D7A7B115.svg";
        public const string ICON_FILE = "https://files.getquicker.net/_icons/350BD13EC689E5FAF652DFC71352CAD8CFF3733E.svg";
        public const string ICON_FOLDER = "https://files.getquicker.net/_icons/A63E72A6728B560400E822969044DB7A00ACAC28.svg";
        public const string ICON_RICHTEXT = "https://files.getquicker.net/_icons/66F24B7ED461247C52DB9E32EF9B801F97A090B3.svg";
        public const string ICON_EARTH = "https://files.getquicker.net/_icons/99A6D79D46C1F1E67869507DFA7D7032F6EA63E4.svg";
        #endregion
    }
}

