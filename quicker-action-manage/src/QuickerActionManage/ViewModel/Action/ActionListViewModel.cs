using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using log4net;
using Newtonsoft.Json;
using Quicker.Common;
using Quicker.Public.Extensions;
using Quicker.View;
using Quicker.Utilities._3rd;
using QuickerActionManage.State;
using QuickerActionManage.Utils;

namespace QuickerActionManage.ViewModel
{
    public class ActionListViewModel : ListModel, IDisposable
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

            ActivateRule();

            PropertyChanged += ActionListViewModel_PropertyChanged;
            GSModel.PropertyChanged += GSModel_PropertyChanged;
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
            _logger.Info(JsonConvert.SerializeObject(usageInfo, Formatting.Indented));
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

        private bool AdvanceFilter(ActionItemModel item)
        {
            if (_referencedActionIds == null)
            {
                return true;
            }
            else
            {
                return _referencedActionIds.Contains(item.Id);
            }
        }

        private ActionRuleModel? _lastSelectedRule = null;
        private ActionRuleModel LastRule => _lastSelectedRule ?? DefaultRule;

        private void ActionListViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentRule):
                    ActivateRule();
                    _lastSelectedRule = CurrentRule;
                    break;
                default:
                    break;
            }
        }

        private void ActivateRule()
        {
            LastRule.Sorter.PropertyChanged -= Sorter_PropertyChanged;
            LastRule.FilterItem.PropertyChanged -= FilterItem_PropertyChanged;
            Sorter.PropertyChanged += Sorter_PropertyChanged;
            FilterItem.PropertyChanged += FilterItem_PropertyChanged;
            ReSort();
        }

        private void FilterItem_PropertyChanged(object sender, PropertyChangedEventArgs e) => Refresh();
        private void Sorter_PropertyChanged(object sender, PropertyChangedEventArgs e) => ReSort();

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

        public ActionItemModel? SelectedItem { get; set; }

        static ActionListViewModel()
        {
            var wt = new GlobalStateWriter(typeof(ActionListViewModel).FullName);
            var data = wt.Read(nameof(Rules)) as string;
            try
            {
                Rules = JsonConvert.DeserializeObject<QuickerActionManage.Utils.FullyObservableCollection<ActionRuleModel>>(data ?? "null") ?? new QuickerActionManage.Utils.FullyObservableCollection<ActionRuleModel>();
            }
            catch
            {
                Rules = new();
            }

            Rules.CollectionChanged += (s, e) => wt.Write(nameof(Rules), Rules);
            Rules.ItemPropertyChanged += (s, e) => wt.Write(nameof(Rules), Rules);
        }

        private static QuickerActionManage.Utils.FullyObservableCollection<ActionRuleModel> Rules { get; }

        public QuickerActionManage.Utils.FullyObservableCollection<ActionRuleModel> RuleItems => Rules;

        public ActionRuleModel? SelectedRule { get; set; }
        public ActionRuleModel DefaultRule { get; set; } = ActionRuleModel.Create();
        public ActionRuleModel CurrentRule => SelectedRule ?? DefaultRule;
        public ActionItemFilter FilterItem => CurrentRule.FilterItem;
        public ActionItemSorter Sorter => CurrentRule.Sorter;
        public void SetDefaultRule()
        {
            SelectedRule = null;
            GSModel.SelectedItem = null;
            DefaultRule = ActionRuleModel.Create();
        }
        public void SaveRule()
        {
            if (SelectedRule != null)
            {
                return;
            }

            var rule = new ActionRuleModel()
            {
                Title = "请输入名字",
                FilterItem = FilterItem.Clone(),
                Sorter = Sorter.Clone()
            };

            Rules.Add(rule);
            SelectedRule = rule;
        }

        protected override CollectionView GetView() => _view;

        protected override IEnumerable<SortDescription> GetSortDescriptions() => Sorter.GetSortDescription();

        public void Dispose()
        {
            GSModel.Dispose();
        }

        public GlobalSubprogramListModel GSModel { get; } = new();

        public IList<string> ExeNameList => ActionItemFilter.ExeNameList;
    }
}

