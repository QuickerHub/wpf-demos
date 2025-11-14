using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Data;
using Newtonsoft.Json;
using QuickerActionManage.Utils.Extension;

namespace QuickerActionManage.ViewModel
{
    public class ActionItemSorter : Sorter
    {
        [DisplayName("排序方式")]
        public ActionSortType SortType { get; set; } = ActionSortType.LastEditTime;

        [Browsable(false)]
        public ListSortDirection SortDirection { get; set; } = ListSortDirection.Descending;

        [DisplayName("升序")]
        public bool Ascending
        {
            get => SortDirection == ListSortDirection.Ascending;
            set => SortDirection = value ? ListSortDirection.Ascending : ListSortDirection.Descending;
        }

        [DisplayName("降序")]
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
                    yield return new SortDescription(SortType.ToString(), SortDirection);
                    yield return new SortDescription(nameof(ActionSortType.CreateTime), ListSortDirection.Descending);
                    break;
                case ActionSortType.CreateTime:
                case ActionSortType.Title:
                case ActionSortType.Size:
                case ActionSortType.UsageCount:
                case ActionSortType.ExeName:
                case ActionSortType.Description:
                    yield return new SortDescription(SortType.ToString(), SortDirection);
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

