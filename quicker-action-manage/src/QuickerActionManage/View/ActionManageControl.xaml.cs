using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickerActionManage.View.Menus;
using QuickerActionManage.ViewModel;
using QuickerActionManage.Utils;
using QuickerActionManage.Utils.Extension;
using static QuickerActionManage.View.Menus.MenuFactory;

namespace QuickerActionManage.View
{
    public static class ListExtensions
    {
        public static int FirstIndexOf<T>(this System.Collections.IList list, Func<T, bool> predicate) where T : class
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is T item && predicate(item))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}

namespace QuickerActionManage.View
{
    /// <summary>
    /// ActionManageControl.xaml 的交互逻辑
    /// </summary>
    public partial class ActionManageControl : UserControl
    {
        public ActionListViewModel ViewModel { get; } = new();
        public ActionManageControl()
        {
            InitializeComponent();
            DataContext = ViewModel;
            CommandBindings.AddKeyGesture(new KeyGesture(Key.Delete), (s, e) => _ = DeleteActionAsync(TheListView.SelectedItems.Cast<ActionItemModel>().ToList()));
            this.Loaded += (s, e) =>
            {
                if (Window.GetWindow(this) is Window win)
                {
                    win.Closed += (s, e) => ViewModel.Dispose();
                }
            };

            AddHandler(Button.ClickEvent, new RoutedEventHandler(ButtonClick));
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader header)
            {
                ActionSortType? tt = (string)header.Content switch
                {
                    "大小" => ActionSortType.Size,
                    "最后编辑" => ActionSortType.LastEditTime,
                    "最后分享" => ActionSortType.ShareTime,
                    "创建时间" => ActionSortType.CreateTime,
                    "标题" => ActionSortType.Title,
                    "使用次数" => ActionSortType.UsageCount,
                    "进程" => ActionSortType.ExeName,
                    "描述" => ActionSortType.Description,
                    _ => null,
                };
                if (tt is ActionSortType st1)
                {
                    if (st1 == ViewModel.Sorter.SortType)
                    {
                        ViewModel.Sorter.Ascending ^= true;
                    }
                    else
                    {
                        ViewModel.Sorter.SortType = st1;
                    }
                }
            }
        }

        private void TheListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (UIHelper.FindVisualParent<ListViewItem>((DependencyObject)e.OriginalSource) != null)
            {
                ViewModel.SelectedItem?.Edit();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => ViewModel.SetUpActions();

        private void TheListView_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var menu = TheListView.ContextMenu;
            menu.Items.Clear();
            if (UIHelper.FindVisualParent<ListViewItem>((DependencyObject)e.OriginalSource) is not null)
            {
                if (TheListView.SelectedItems == null)
                    return;
                if (TheListView.SelectedItems.Count > 1)
                {
                    menu.AddChildMenu(GetMultiSelectMenu(TheListView.SelectedItems.Cast<ActionItemModel>().ToList()));
                    return;
                }
                if (ViewModel.SelectedItem is ActionItemModel item)
                {
                    QuickerUtil.CreateActionMenus(menu, item.Id, Window.GetWindow(this));
                    var idx = ListExtensions.FirstIndexOf<MenuItem>(menu.Items, x => x is MenuItem mitem && (string)mitem.Header == "删除");
                    if (idx != -1)
                    {
                        menu.Items.RemoveAt(idx);
                        menu.Items.Insert(idx, CreateMenuItem(ICON_DELETE, "删除", async (s, e) =>
                        {
                            var removed = await QuickerUtil.DeleteAction(item.Id);
                            if (removed)
                            {
                                ViewModel.ActionItems.Remove(item);
                            }
                        }));
                    }
                }
            }
        }

        private IEnumerable<Control> GetMultiSelectMenu(IList<ActionItemModel> items)
        {
            static void CopyItems(IEnumerable<string> s)
            {
                Clipboard.SetText(string.Join(Environment.NewLine, s));
            }

            yield return CreateMenuItem(ICON_COPY, "复制动作ID", (s, e) => CopyItems(items.Select(x => x.Id)));

            yield return CreateMenuItem(ICON_DELETE, "删除", (s, e) => _ = DeleteActionAsync(items));

            yield break;
        }

        private async Task<bool> DeleteActionAsync(IList<ActionItemModel> items)
        {
            if (items == null || !items.Any())
            {
                return false;
            }

            var ids = items.Select(x => x.Id).ToList();
            var result = MessageBox.Show(Window.GetWindow(this), $"确定要删除共 {ids.Count} 个动作么", "删除动作", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                if (ids.Count >= 10)
                {
                    result = MessageBox.Show(Window.GetWindow(this), "你要删除的动作超过了10个, 请再次确认", "删除动作", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                    if (result != MessageBoxResult.OK)
                    {
                        return false;
                    }
                }

                await QuickerUtil.DeleteAction(items.Select(x => x.Id));
                foreach (var item in items)
                {
                    ViewModel.ActionItems.Remove(item);
                }
                return true;
            }
            return false;
        }

        private async Task<bool> DeleteActionAsync(ActionItemModel? item)
        {
            if (item == null)
            {
                return false;
            }

            var removed = await QuickerUtil.DeleteAction(item.Id);
            if (removed)
            {
                ViewModel.ActionItems.Remove(item);
            }
            return removed;
        }

        private IEnumerable<Control> GetContextMenu(ActionItemModel? item)
        {
            if (item == null)
            {
                yield break;
            }

            yield return CreateMenuItem(ICON_EDIT_PEN, "编辑", (s, e) => QuickerUtil.EditAction(item.Id));

            yield return CreateMenuItem(ICON_COPY, "复制动作ID", (s, e) => Clipboard.SetText(item.Id));

            yield return CreateMenuItem(ICON_DELETE, "删除", async (s, e) => await DeleteActionAsync(item));

            var cmdata = item.Item.ContextMenuData;
            if (!string.IsNullOrWhiteSpace(cmdata))
            {
                yield return new Separator();
                foreach (var m in GetActionContextMenu(item.Id, cmdata))
                {
                    yield return m;
                }
            }
        }

        private void ResetRule_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SetDefaultRule();
            TheRulePop.IsChecked = false;
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedRule != null)
            {
                ViewModel.RuleItems.Remove(ViewModel.SelectedRule);
            }
        }

        private void SaveRule_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveRule();
            RuleNameBox.Focus();
            TheRulePop.IsChecked = false;
        }

        private void TheRuleListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (UIHelper.FindVisualParent<ListBoxItem>((DependencyObject)e.OriginalSource) is not null)
            {
                TheRulePop.IsChecked = false;
            }
        }

        private void RecycleButton_Click(object sender, RoutedEventArgs e)
        {
            QuickerActionManage.Utils.CommonUtil.TryOpenFileOrUrl("quicker://settings:ActionRecycleBinSettingPage");
        }

        private void GSCancel_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.GSModel.SelectedItem = null;
            RefButton.IsChecked = false;
        }

        private void TheSubprogramListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.GSModel.SelectedItem != null)
            {
                RefButton.IsChecked = false;
                e.Handled = true;
            }
        }

        private void RunAction(object sender, RoutedEventArgs e)
        {
            if (((Button)e.OriginalSource).DataContext is ActionRunerModel runner)
            {
                runner.Execute();
            }
        }

        private void DebugAction(object sender, RoutedEventArgs e)
        {
            if (((Button)e.OriginalSource).DataContext is ActionRunerModel runner)
            {
                runner.Debug();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ActionStaticInfo == null)
            {
                // ActionStaticInfo not available (not running in Quicker), skip showing the window
                return;
            }
            
            var page = ViewModel.ActionStaticInfo.Page;
            
            var window = new Window
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Width = 400,
                Height = 300,
                Content = page,
            };

            window.Closed += (s, e) =>
            {
                window.Content = null;
                ViewModel.SetUpActions();
            };
            window.ShowDialog();
        }
    }
}

