using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Design;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickerActionManage.Utils.Extension;
using QuickerActionManage.View.Editor;

namespace QuickerActionManage.ViewModel
{
    public partial class ActionItemSorter : Sorter
    {
        [DisplayName("排序方式")]
        [ObservableProperty]
        public partial ActionSortType SortType { get; set; } = ActionSortType.LastEditTime;

        [DisplayName("排序方向")]
        [Editor(typeof(SortDirectionEditor), typeof(UITypeEditor))]
        [ObservableProperty]
        public partial ListSortDirection SortDirection { get; set; } = ListSortDirection.Descending;

        [Browsable(false)]
        public bool Ascending
        {
            get => SortDirection == ListSortDirection.Ascending;
            set => SortDirection = value ? ListSortDirection.Ascending : ListSortDirection.Descending;
        }

        [Browsable(false)]
        public bool Descending
        {
            get => SortDirection == ListSortDirection.Descending;
            set => SortDirection = value ? ListSortDirection.Descending : ListSortDirection.Ascending;
        }

        public override string Summary
        {
            get
            {
                return $"""排序：{SortType.GetDisplayName()},{(Ascending ? "升序" : "降序")}""";
            }
        }

        public override IEnumerable<SortDescription> GetSortDescription()
        {
            switch (SortType)
            {
                case ActionSortType.LastEditTime:
                case ActionSortType.ShareTime:
                    yield return new SortDescription(SortType.ToString(), (ListSortDirection)SortDirection);
                    yield return new SortDescription(nameof(ActionSortType.CreateTime), ListSortDirection.Descending);
                    break;
                case ActionSortType.CreateTime:
                case ActionSortType.Title:
                case ActionSortType.Size:
                case ActionSortType.UsageCount:
                case ActionSortType.ExeName:
                case ActionSortType.Description:
                    yield return new SortDescription(SortType.ToString(), (ListSortDirection)SortDirection);
                    break;
                case ActionSortType.None:
                    yield break;
                default:
                    break;
            }
        }

        public ActionItemSorter Clone()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<ActionItemSorter>(json) ?? new ActionItemSorter();
        }
    }
}

