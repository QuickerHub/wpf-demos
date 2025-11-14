using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickerActionManage.View.Menus;
using QuickerActionManage.ViewModel;
using QuickerActionManage.Utils;
using static QuickerActionManage.View.Menus.MenuFactory;

namespace QuickerActionManage.View
{
    /// <summary>
    /// SubprogramControl.xaml 的交互逻辑
    /// </summary>
    public partial class SubprogramControl : UserControl
    {
        public GlobalSubprogramListModel ViewModel { get; } = new();
        public SubprogramControl()
        {
            InitializeComponent();
            this.DataContext = ViewModel;
            this.Loaded += (s, e) =>
            {
                if (Window.GetWindow(this) is Window win)
                {
                    win.Closed += (s, e) => ViewModel.Dispose();
                }
            };
        }

        private void TheSubprogramListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (UIHelper.FindVisualParent<ListBoxItem>((DependencyObject)e.OriginalSource) != null)
            {
                if (ViewModel.SelectedItem is SubprogramModel item)
                {
                    QuickerUtil.CreateOrEditGlobalSubprogram(item.Sub);
                }
            }
        }

        private IEnumerable<Control> GetContextMenu(SubprogramModel item)
        {
            yield return CreateMenuItem(ICON_LINK, "查找引用", (s, e) =>
            {
                ViewModel.RefSubprogamId = item.Id;
            });

            yield return CreateMenuItem(ICON_SHARE, "分享", (s, e) =>
            {
                QuickerUtil.ShareSubprogram(item.Sub, Window.GetWindow(this), true);
            });

            if (!string.IsNullOrWhiteSpace(item.SharedId))
            {
                yield return CreateMenuItem(ICON_LINK, "打开分享网址", (s, e) =>
                {
                    var url = @"https://getquicker.net/subprogram?id=" + item.SharedId;
                    QuickerActionManage.Utils.CommonUtil.TryOpenFileOrUrl(url);
                });
            }
        }

        private void TheSubprogramListBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var menu = TheSubprogramListBox.ContextMenu;
            menu.Items.Clear();
            if (UIHelper.IsOnListBoxItem(e.OriginalSource))
            {
                if (TheSubprogramListBox.SelectedItems == null) return;
                if (TheSubprogramListBox.SelectedItems.Count > 1)
                {
                    return;
                }
                if (ViewModel.SelectedItem is SubprogramModel item)
                {
                    menu.AddChildMenu(GetContextMenu(item));
                }
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            QuickerUtil.CreateOrEditGlobalSubprogram(new());
        }

        private void CancelRef_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RefSubprogamId = null;
        }
    }
}

