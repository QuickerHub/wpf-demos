using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Design;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickerActionManage.Utils;
using QuickerActionManage.Utils.Extension;
using QuickerActionManage.View.Editor;

namespace QuickerActionManage.ViewModel
{
    public partial class ActionItemFilter : NObject
    {
        [DisplayName("动作类型")]
        [ObservableProperty]
        public partial ActionType1 ActionType { get; set; }

        [Browsable(false)]
        [ObservableProperty]
        public partial string? SearchText { get; set; }

        [DisplayName("最小(kb)")]
        [NumEditorProperty(0, double.MaxValue, 10)]
        [ObservableProperty]
        public partial double MinSize { get; set; } = 0;

        [DisplayName("最大(kb)")]
        [NumEditorProperty(0, double.MaxValue, 10)]
        [ObservableProperty]
        public partial double MaxSize { get; set; } = 10000;

        public enum ActionFroms
        {
            [Display(Name = "所有")]
            All,
            [Display(Name = "自己写的")]
            Self,
            [Display(Name = "安装的")]
            Others,
        }

        [Browsable(false)]
        [ObservableProperty]
        public partial ActionFroms ActionFrom { get; set; }

        [DisplayName("仅自己写的")]
        public bool SelfAction 
        { 
            get => ActionFrom == ActionFroms.Self; 
            set => ActionFrom = value ? ActionFroms.Self : ActionFroms.All; 
        }

        [DisplayName("已分享")]
        [ObservableProperty]
        public partial bool Shared { get; set; }

        [DisplayName("网址动作")]
        [ObservableProperty]
        public partial bool IsAssUrl { get; set; }

        [DisplayName("仅安装的")]
        public bool OtherAction 
        { 
            get => ActionFrom == ActionFroms.Others; 
            set => ActionFrom = value ? ActionFroms.Others : ActionFroms.All; 
        }

        [DisplayName("仅自动更新")]
        [ObservableProperty]
        public partial bool AutoUpdateAction { get; set; }

        [DisplayName("使用次数 > 0")]
        [ObservableProperty]
        public partial bool Used { get; set; }

        [Browsable(false)]
        public static IList<string> ExeNameList { get; set; } = new List<string>();

        [DisplayName("进程")]
        [PropertyBindingAttribute(typeof(ActionItemFilter), ItemsSourceName = nameof(ExeNameList))]
        [Editor(typeof(ComboBoxPropertyEditor), typeof(UITypeEditor))]
        [ObservableProperty]
        public partial string? SelectedExeName { get; set; }

        public bool Filter(ActionItemModel item)
        {
            return (ActionType == ActionType1.All || item.ActionType == ActionType)
                && FromFilter(item)
                && (!AutoUpdateAction || item.AutoUpdate)
                && (!Used || item.UsageCount > 0)
                && (!Shared || item.Shared)
                && (!IsAssUrl || item.IsAssUrl)
                && (string.IsNullOrEmpty(SelectedExeName) || item.ExeName == SelectedExeName)
                && TextUtil.Search(SearchText, item.Title, item.Description, item.ExeName)
                && (MinSize <= item.SizeKb && item.SizeKb <= MaxSize);
        }
        public bool FromFilter(ActionItemModel item)
        {
            return ActionFrom == ActionFroms.All
                || (string.IsNullOrEmpty(item.TemplateId) ? ActionFrom == ActionFroms.Self : ActionFrom == ActionFroms.Others);
        }

        public override string Summary
        {
            get
            {
                var parts = new List<string>
                {
                    $"类型：{ActionType.GetDisplayName()}",
                    $"大小：{MinSize}-{MaxSize} kb",
                    $"来源：{ActionFrom.GetDisplayName()}"
                };
                if (!string.IsNullOrEmpty(SelectedExeName))
                {
                    parts.Add($"进程：{SelectedExeName}");
                }
                return string.Join("   ", parts);
            }
        }

        public void ResetToDefault()
        {
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(new ActionItemFilter()), this);
        }

        public ActionItemFilter Clone()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<ActionItemFilter>(json) ?? new ActionItemFilter();
        }
    }
}

