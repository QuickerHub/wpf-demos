using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Data;
using QuickerActionManage.Utils.Extension;
using PropertyChanged;

namespace QuickerActionManage.ViewModel
{
    public class SubprogramSorter : Sorter
    {
        [DisplayName("排序方式")]
        [OnChangedMethod(nameof(OnSortTypeChanged))]
        public SubprogramSortType SortType { get; set; }
        private void OnSortTypeChanged()
        {
            SortDirection = SortType switch
            {
                SubprogramSortType.Name => ListSortDirection.Ascending,
                _ => ListSortDirection.Descending
            };
        }

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

        public override string Summary => $"""排序：{SortType.GetDisplayName()},{(Ascending ? "升序" : "降序")}""";
        public override IEnumerable<SortDescription> GetSortDescription()
        {
            switch (SortType)
            {
                case SubprogramSortType.LastEditTime:
                case SubprogramSortType.ShareTime:
                    yield return new SortDescription(SortType.ToString(), SortDirection);
                    yield return new SortDescription(nameof(SubprogramSortType.CreateTime), ListSortDirection.Descending);
                    break;
                case SubprogramSortType.CreateTime:
                case SubprogramSortType.Name:
                    yield return new SortDescription(SortType.ToString(), SortDirection);
                    break;
                default:
                    break;
            }
        }
    }
}

