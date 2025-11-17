using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
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
            CommandBindings.AddKeyGesture(new KeyGesture(Key.F5), (s, e) => ViewModel.SetUpActions());
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
                    AddGroupMenuItems(menu, item);
                    ReplaceDeleteMenuItem(menu, item);
                }
            }
        }

        /// <summary>
        /// Create "Add to Group" menu item
        /// </summary>
        private Control CreateAddToGroupMenuItem(IList<ActionItemModel> items)
        {
            return CreateMenuItem(
                icon: "fa:Light_Folder",
                title: "添加到分组",
                children: GetAddToGroupMenuItems(items));
        }

        /// <summary>
        /// Create "Remove from Current Group" menu item
        /// Only shown when current group is not "All" and actions are in the current group
        /// </summary>
        private Control? CreateRemoveFromGroupMenuItem(IList<ActionItemModel> items)
        {
            // Only show in non-"All" groups
            var currentGroup = ViewModel.SelectedGroup;
            if (currentGroup == null || currentGroup.IsAllGroup)
            {
                return null;
            }

            // Check if any of the selected actions are in the current group
            var actionIds = items.Select(x => x.Id).ToHashSet();
            var groupActionIds = ViewModel.GroupManager.GetActionIdsForGroup(currentGroup);
            if (groupActionIds == null || !groupActionIds.Any(actionId => actionIds.Contains(actionId)))
            {
                return null; // No actions in current group
            }

            return CreateMenuItem(
                icon: "fa:Light_FolderMinus",
                title: "从当前分组中移除",
                handler: (s, e) => RemoveActionsFromGroup(items, currentGroup));
        }

        /// <summary>
        /// Add "Add to Group" and "Remove from Group" menu items before the delete menu item
        /// </summary>
        private void AddGroupMenuItems(ContextMenu menu, ActionItemModel item)
        {
            // Find the delete menu item index to insert before it
            var deleteIdx = ListExtensions.FirstIndexOf<MenuItem>(menu.Items, x => x is MenuItem mitem && (string)mitem.Header == "删除");
            if (deleteIdx == -1)
            {
                // If no delete menu item found, append to the end
                deleteIdx = menu.Items.Count;
            }

            // Create a list with a single item for the menu items generator
            var items = new List<ActionItemModel> { item };
            
            // Create the "Add to Group" menu item
            var addToGroupMenuItem = CreateAddToGroupMenuItem(items);
            menu.Items.Insert(deleteIdx, addToGroupMenuItem);
            deleteIdx++; // Update index after insertion

            // Create the "Remove from Group" menu item (if applicable)
            var removeFromGroupMenuItem = CreateRemoveFromGroupMenuItem(items);
            if (removeFromGroupMenuItem != null)
            {
                menu.Items.Insert(deleteIdx, removeFromGroupMenuItem);
            }
        }

        /// <summary>
        /// Replace the default delete menu item with a custom one that removes the item from the view model
        /// If the delete menu item doesn't exist, add it to the menu
        /// </summary>
        private void ReplaceDeleteMenuItem(ContextMenu menu, ActionItemModel item)
        {
            var deleteMenuItem = CreateMenuItem(ICON_DELETE, "删除", async (s, e) =>
            {
                var result = MessageBox.Show(Window.GetWindow(this), $"确定要删除动作 \"{item.Title}\" 吗？", "删除动作", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.OK)
                {
                    var removed = await QuickerUtil.DeleteAction(item.Id);
                    if (removed)
                    {
                        ViewModel.ActionItems.Remove(item);
                    }
                }
            });

            var idx = ListExtensions.FirstIndexOf<MenuItem>(menu.Items, x => x is MenuItem mitem && (string)mitem.Header == "删除");
            if (idx != -1)
            {
                // Replace existing delete menu item
                menu.Items.RemoveAt(idx);
                menu.Items.Insert(idx, deleteMenuItem);
            }
            else
            {
                // Add delete menu item if it doesn't exist
                menu.Items.Add(deleteMenuItem);
            }
        }

        private IEnumerable<Control> GetMultiSelectMenu(IList<ActionItemModel> items)
        {
            static void CopyItems(IEnumerable<string> s)
            {
                Clipboard.SetText(string.Join(Environment.NewLine, s));
            }

            yield return CreateMenuItem(ICON_COPY, "复制动作ID", (s, e) => CopyItems(items.Select(x => x.Id)));

            // 添加到分组菜单
            yield return CreateAddToGroupMenuItem(items);

            // 从分组中移除菜单
            var removeFromGroupMenuItem = CreateRemoveFromGroupMenuItem(items);
            if (removeFromGroupMenuItem != null)
            {
                yield return removeFromGroupMenuItem;
            }

            yield return CreateMenuItem(ICON_DELETE, "删除", (s, e) => _ = DeleteActionAsync(items));

            yield break;
        }

        private IEnumerable<Control> GetAddToGroupMenuItems(IList<ActionItemModel> items)
        {
            // 添加所有分组（排除"全部"分组）
            foreach (var group in ViewModel.GroupManager.Groups.Where(g => !g.IsAllGroup))
            {
                yield return CreateMenuItem(
                    icon: string.Empty,
                    title: group.Name,
                    handler: (s, e) => AddActionsToGroup(items, group));
            }

            // 如果有分组，添加分隔线
            if (ViewModel.GroupManager.Groups.Any(g => !g.IsAllGroup))
            {
                yield return new Separator();
            }

            // 添加"新分组"选项
            yield return CreateMenuItem(
                icon: ICON_PLUS,
                title: "新分组",
                handler: (s, e) => AddActionsToNewGroup(items));
        }

        private void AddActionsToGroup(IList<ActionItemModel> items, ViewModel.ActionGroup group)
        {
            var actionIds = items.Select(x => x.Id).ToList();
            foreach (var actionId in actionIds)
            {
                // AddActionToGroup 方法内部已经有去重逻辑
                ViewModel.GroupManager.AddActionToGroup(group, actionId);
            }
            
            // If the current selected group is the target group, refresh the view
            if (ViewModel.SelectedGroup == group)
            {
                ViewModel.Refresh();
            }
        }

        private void AddActionsToNewGroup(IList<ActionItemModel> items)
        {
            // 创建新分组
            var groupCount = ViewModel.GroupManager.Groups.Count(g => !g.IsAllGroup);
            var group = ViewModel.GroupManager.AddGroup($"分组{groupCount + 1}");

            // 先添加动作到新分组
            AddActionsToGroup(items, group);
            
            // 然后切换分组（这会触发刷新）
            ViewModel.SelectedGroup = group;
        }

        private void RemoveActionsFromGroup(IList<ActionItemModel> items, ViewModel.ActionGroup group)
        {
            var actionIds = items.Select(x => x.Id).ToList();
            foreach (var actionId in actionIds)
            {
                ViewModel.GroupManager.RemoveActionFromGroup(group, actionId);
            }
            
            // If the current selected group is the target group, update filter and refresh the view
            if (ViewModel.SelectedGroup == group)
            {
                // Update group filter to reflect the removed actions
                ViewModel.UpdateGroupFilter();
            }
        }

        private async Task<bool> DeleteActionAsync(IList<ActionItemModel> items)
        {
            if (items == null || !items.Any())
            {
                return false;
            }

            var ids = items.Select(x => x.Id).ToList();
            var result = MessageBox.Show(Window.GetWindow(this), $"确定要删除共 {ids.Count} 个动作吗？", "删除动作", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                if (ids.Count >= 10)
                {
                    result = MessageBox.Show(Window.GetWindow(this), $"你要删除的动作超过了10个，请再次确认。", "删除动作", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
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

            var result = MessageBox.Show(Window.GetWindow(this), $"确定要删除动作 \"{item.Title}\" 吗？", "删除动作", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK)
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

        private void AddRule_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddRule();
            // Focus will be handled by RuleNameTextBox_IsVisibleChanged or RuleNameTextBox_Loaded
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedRule != null)
            {
                ViewModel.RuleItems.Remove(ViewModel.SelectedRule);
            }
        }

        private void TheRuleListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Rule selection is now always visible, no need to close popup
        }
        
        private void TheRuleListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selection was cleared (removed but nothing added), and we have a SelectedRule in ViewModel,
            // restore it to prevent accidental clearing when clicking on filter controls
            if (e.RemovedItems.Count > 0 && e.AddedItems.Count == 0 && ViewModel.SelectedRule != null)
            {
                // Only restore if the removed item was our SelectedRule
                if (e.RemovedItems.Contains(ViewModel.SelectedRule))
                {
                    // Restore the selection
                    if (sender is ListBox listBox)
                    {
                        listBox.SelectedItem = ViewModel.SelectedRule;
                    }
                }
            }
        }

        private void RuleListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as System.Windows.Controls.ListBoxItem;
            if (listBoxItem == null) return;

            var rule = listBoxItem.DataContext as ActionRuleModel;
            if (rule == null) return;

            StartRuleEditing(rule);
            e.Handled = true;
        }

        private void RenameRuleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedRule != null)
            {
                StartRuleEditing(ViewModel.SelectedRule);
            }
        }

        private void StartRuleEditing(ActionRuleModel rule)
        {
            ViewModel.StartEditingRule(rule);
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

        private void TheGroupListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Group selection is handled by binding, no additional action needed
        }

        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            // Count only non-"All" groups for naming
            var groupCount = ViewModel.GroupManager.Groups.Count(g => !g.IsAllGroup);
            var group = ViewModel.GroupManager.AddGroup($"分组{groupCount + 1}");
            ViewModel.SelectedGroup = group;
            // The EditableTextBlock will handle editing via double-click
            // If auto-edit is needed, it can be implemented in EditableTextBlock's Loaded event
        }

        private void DeleteGroupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedGroup == null || ViewModel.SelectedGroup.IsAllGroup)
            {
                return; // Cannot delete "All" group
            }

            var groupToDelete = ViewModel.SelectedGroup;
            
            // Check if group has actions, and ask for confirmation
            var groupActionIds = ViewModel.GroupManager.GetActionIdsForGroup(groupToDelete);
            if (groupActionIds != null && groupActionIds.Count > 0)
            {
                var result = MessageBox.Show(
                    Window.GetWindow(this),
                    $"分组 \"{groupToDelete.Name}\" 中包含 {groupActionIds.Count} 个动作。\n\n删除分组不会删除动作，只会移除分组关系。\n\n确定要删除该分组吗？",
                    "删除分组",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);
                
                if (result != MessageBoxResult.OK)
                {
                    return; // User cancelled
                }
            }
            
            // Switch to "All" group before deleting
            ViewModel.SelectedGroup = ViewModel.GroupManager.AllGroup;
            
            // Remove the group
            ViewModel.GroupManager.RemoveGroup(groupToDelete);
        }
    }
}

