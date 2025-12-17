using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Quicker.Common;
using QuickerActionManage.Utils;

namespace QuickerActionManage.ViewModel
{
    /// <summary>
    /// ViewModel for action data optimization analysis
    /// </summary>
    public partial class ActionDataOptimizeViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ActionDataItem> _actionDataItems = new();

        public int TotalActions => ActionDataItems.Count;

        public long TotalDataSize => ActionDataItems.Sum(x => x.DataSize);

        public long TotalOptimizedSize => ActionDataItems.Sum(x => x.OptimizedDataSize);

        public long TotalSavedSize => TotalDataSize - TotalOptimizedSize;

        public double TotalSavedPercentage => TotalDataSize > 0 ? (TotalSavedSize * 100.0 / TotalDataSize) : 0;

        /// <summary>
        /// Load action data and calculate optimization for all actions
        /// </summary>
        public void LoadActions()
        {
            if (!QuickerUtil.CheckIsInQuicker())
            {
                return;
            }

            var allActions = QuickerUtil.GetAllActionItems();
            var actionIds = allActions.Select(a => a.Id).ToList();
            LoadActionsByIds(actionIds);
        }

        /// <summary>
        /// Load action data and calculate optimization for specified action IDs
        /// </summary>
        /// <param name="actionIds">List of action IDs to load</param>
        public void LoadActionsByIds(IEnumerable<string> actionIds)
        {
            ActionDataItems.Clear();

            if (!QuickerUtil.CheckIsInQuicker())
            {
                return;
            }

            foreach (var actionId in actionIds)
            {
                var action = QuickerUtil.GetActionById(actionId);
                if (action == null)
                {
                    continue;
                }

                var data = new[] { action.Data, action.Data2, action.Data3 }
                    .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

                if (data == null)
                {
                    continue;
                }

                var dataSize = data.Length;
                var optimizedData = JsonHelper.RemoveNullProperties(data);
                var optimizedDataSize = optimizedData?.Length ?? 0;

                var item = new ActionDataItem
                {
                    Icon = action.Icon,
                    Title = action.Title,
                    Size = action.Data?.Length ?? 0,
                    DataSize = dataSize,
                    OptimizedDataSize = optimizedDataSize,
                    SavedSize = dataSize - optimizedDataSize,
                    SavedPercentage = dataSize > 0 ? ((dataSize - optimizedDataSize) * 100.0 / dataSize) : 0
                };

                ActionDataItems.Add(item);
            }

            OnPropertyChanged(nameof(TotalActions));
            OnPropertyChanged(nameof(TotalDataSize));
            OnPropertyChanged(nameof(TotalOptimizedSize));
            OnPropertyChanged(nameof(TotalSavedSize));
            OnPropertyChanged(nameof(TotalSavedPercentage));
        }
    }

    /// <summary>
    /// Item for displaying action data statistics
    /// </summary>
    public class ActionDataItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Size { get; set; }
        public int DataSize { get; set; }
        public int OptimizedDataSize { get; set; }
        public int SavedSize { get; set; }
        public double SavedPercentage { get; set; }
    }
}

