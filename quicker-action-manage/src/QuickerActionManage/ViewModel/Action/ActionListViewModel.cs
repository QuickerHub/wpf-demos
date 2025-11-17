using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using log4net;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Quicker.Common;
using Quicker.Public.Extensions;
using Quicker.View;
using Quicker.Utilities._3rd;
using QuickerActionManage.State;
using QuickerActionManage.Utils;

namespace QuickerActionManage.ViewModel
{
    public partial class ActionListViewModel : ListModel, IDisposable
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(ActionListViewModel));
        private readonly ActionStaticInfo? _actionStaticInfo;
        public ActionStaticInfo? ActionStaticInfo => _actionStaticInfo;
        
        public ActionListViewModel()
        {
            // Only create ActionStaticInfo when running in Quicker
            if (QuickerUtil.CheckIsInQuicker())
            {
                try
                {
                    _actionStaticInfo = new ActionStaticInfo();
                }
                catch (Exception ex)
                {
                    _logger.Warn("Failed to create ActionStaticInfo", ex);
                    _actionStaticInfo = null;
                }
            }
            
            SetUpActions();
            Task.Run(async () =>
            {
                await Task.Delay(500); // 等待Quicker加载完成
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SetUpActions();
                });
            });

            _view = (CollectionView)CollectionViewSource.GetDefaultView(ActionItems);

            _view.Filter = obj =>
            {
                var item = (ActionItemModel)obj;
                return FilterItem.Filter(item)
                       && AdvanceFilter(item);
            };

            // Subscribe to initial rule's events
            SubscribeRuleEvents(CurrentRule);

            GSModel.PropertyChanged += GSModel_PropertyChanged;

            // Initialize with "All" group selected
            SelectedGroup = GroupManager.AllGroup;
            
            // Post-initialize GroupManager to subscribe to events
            GroupManager.PostInit();
        }
        
        public IList<ActionItemModel> GetItems()
        {
            Dictionary<string, ActionCountItem> usageInfo;
            if (_actionStaticInfo != null)
            {
                usageInfo = _actionStaticInfo.GetActionCountItems()
                    .ToDictionary(x => x.Id, x => x);
            }
            else
            {
                usageInfo = new Dictionary<string, ActionCountItem>();
            }
            _logger.Debug($"Loaded usage info for {usageInfo.Count} actions");
            var items = QuickerUtil.GetAllActionItems()
                .Select(x =>
                {
                    var count = usageInfo.TryGetValue(x.Id, out var info) ? info.Count : 0;
                    return new ActionItemModel(x) { UsageCount = count };
                })
                .ToList();
            return items;
        }

        private void GSModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(GSModel.SelectedItem):
                    if (GSModel.SelectedItem is SubprogramModel sub)
                    {
                        _referencedActionIds = new(ActionItems.Where(x => x.Item.Data?.Contains(sub.Id)
                                                                              ?? false).Select(x => x.Id));
                    }
                    else
                    {
                        _referencedActionIds = null;
                    }
                    Refresh();
                    break;
                default:
                    break;
            }
        }

        private HashSet<string>? _referencedActionIds;
        private HashSet<string>? _groupActionIds;

        private bool AdvanceFilter(ActionItemModel item)
        {
            // Filter by group
            if (_groupActionIds != null && !_groupActionIds.Contains(item.Id))
            {
                return false;
            }

            // Filter by referenced actions
            if (_referencedActionIds == null)
            {
                return true;
            }
            else
            {
                return _referencedActionIds.Contains(item.Id);
            }
        }


        private void FilterItem_PropertyChanged(object sender, PropertyChangedEventArgs e) => Refresh();
        private void Sorter_PropertyChanged(object sender, PropertyChangedEventArgs e) => ReSort();
        
        private void SubscribeRuleEvents(ActionRuleModel rule)
        {
            rule.Sorter.PropertyChanged += Sorter_PropertyChanged;
            rule.FilterItem.PropertyChanged += FilterItem_PropertyChanged;
        }
        
        private void UnsubscribeRuleEvents(ActionRuleModel rule)
        {
            rule.Sorter.PropertyChanged -= Sorter_PropertyChanged;
            rule.FilterItem.PropertyChanged -= FilterItem_PropertyChanged;
        }

        [JsonIgnore]
        public SmartCollection<ActionItemModel> ActionItems { get; } = new();

        public void UpdateExeNameInfo()
        {
            ActionItemFilter.ExeNameList = ActionItems.Select(x => x.ExeName).Distinct().ToList();
        }

        private readonly CollectionView _view;

        public void SetUpActions()
        {
            ActionItems.Reset(GetItems());
            UpdateExeNameInfo();
        }

        [ObservableProperty]
        public partial ActionItemModel? SelectedItem { get; set; }

        static ActionListViewModel()
        {
            var wt = new GlobalStateWriter(typeof(ActionListViewModel).FullName);
            // Use different key for debug mode to avoid mixing test data with real data
            var rulesKey = QuickerUtil.CheckIsInQuicker() ? nameof(Rules) : $"{nameof(Rules)}_Debug";
            var data = wt.Read(rulesKey) as string;
            try
            {
                Rules = JsonConvert.DeserializeObject<QuickerActionManage.Utils.FullyObservableCollection<ActionRuleModel>>(data ?? "null") ?? new QuickerActionManage.Utils.FullyObservableCollection<ActionRuleModel>();
            }
            catch
            {
                Rules = new();
            }

            Rules.CollectionChanged += (s, e) => wt.Write(rulesKey, Rules);
            Rules.ItemPropertyChanged += (s, e) => wt.Write(rulesKey, Rules);
        }

        private static QuickerActionManage.Utils.FullyObservableCollection<ActionRuleModel> Rules { get; }

        public QuickerActionManage.Utils.FullyObservableCollection<ActionRuleModel> RuleItems => Rules;

        [ObservableProperty]
        public partial ActionRuleModel? SelectedRule { get; set; }
        
        [ObservableProperty]
        public partial ActionRuleModel DefaultRule { get; set; } = ActionRuleModel.Create();
        
        public ActionRuleModel CurrentRule => SelectedRule ?? DefaultRule;
        public ActionItemFilter FilterItem => CurrentRule.FilterItem;
        public ActionItemSorter Sorter => CurrentRule.Sorter;
        
        /// <summary>
        /// Currently editing ActionRuleModel (for rename)
        /// </summary>
        [ObservableProperty]
        public partial ActionRuleModel? EditingRule { get; set; }
        
        partial void OnSelectedRuleChanged(ActionRuleModel? oldValue, ActionRuleModel? newValue)
        {
            UnsubscribeRuleEvents(oldValue ?? DefaultRule);
            SubscribeRuleEvents(newValue ?? DefaultRule);
            
            OnPropertyChanged(nameof(CurrentRule));
            OnPropertyChanged(nameof(FilterItem));
            OnPropertyChanged(nameof(Sorter));
            ReSort();
        }
        
        partial void OnDefaultRuleChanged(ActionRuleModel oldValue, ActionRuleModel newValue)
        {
            if (SelectedRule == null)
            {
                UnsubscribeRuleEvents(oldValue);
                SubscribeRuleEvents(newValue);
                ReSort();
            }
        }
        
        /// <summary>
        /// Start editing a rule's name
        /// </summary>
        public void StartEditingRule(ActionRuleModel rule)
        {
            EditingRule = rule;
        }

        /// <summary>
        /// Cancel editing rule name
        /// </summary>
        public void CancelEditingRule()
        {
            EditingRule = null;
        }
        
        public void SetDefaultRule()
        {
            SelectedRule = null;
            GSModel.SelectedItem = null;
            DefaultRule = ActionRuleModel.Create();
        }
        
        public void AddRule()
        {
            var rule = ActionRuleModel.Create();
            rule.Title = $"规则{Rules.Count + 1}";

            Rules.Add(rule);
            SelectedRule = rule;
            
            // Automatically start editing the new rule's name
            EditingRule = rule;
        }

        protected override CollectionView GetView() => _view;

        protected override IEnumerable<SortDescription> GetSortDescriptions() => Sorter.GetSortDescription();

        public void Dispose()
        {
            GSModel.Dispose();
        }

        public GlobalSubprogramListModel GSModel { get; } = new();

        /// <summary>
        /// Action group manager
        /// </summary>
        public ActionGroupManager GroupManager { get; } = new();

        /// <summary>
        /// Currently selected group
        /// </summary>
        [ObservableProperty]
        public partial ActionGroup? SelectedGroup { get; set; }

        partial void OnSelectedGroupChanged(ActionGroup? oldValue, ActionGroup? newValue)
        {
            // Update filter based on selected group
            _groupActionIds = GroupManager.GetActionIdsForGroup(newValue);
            Refresh();
        }

        /// <summary>
        /// Update group filter based on current selected group
        /// Call this when group's ActionIds collection changes
        /// </summary>
        public void UpdateGroupFilter()
        {
            _groupActionIds = GroupManager.GetActionIdsForGroup(SelectedGroup);
            Refresh();
        }
    }
}

