using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickerActionManage.Utils.Extension;

namespace QuickerActionManage.ViewModel
{
    public partial class SubprogramSorter : Sorter
    {
        [DisplayName("排序方式")]
        [ObservableProperty]
        public partial SubprogramSortType SortType { get; set; }
        
        partial void OnSortTypeChanged(SubprogramSortType value)
        {
            SortDirection = value switch
            {
                SubprogramSortType.Name => ListSortDirection.Ascending,
                _ => ListSortDirection.Descending
            };
        }

        [Browsable(false)]
        [ObservableProperty]
        public partial ListSortDirection SortDirection { get; set; } = ListSortDirection.Descending;

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
                    yield return new SortDescription(SortType.ToString(), (ListSortDirection)SortDirection);
                    yield return new SortDescription(nameof(SubprogramSortType.CreateTime), ListSortDirection.Descending);
                    break;
                case SubprogramSortType.CreateTime:
                case SubprogramSortType.Name:
                    yield return new SortDescription(SortType.ToString(), (ListSortDirection)SortDirection);
                    break;
                default:
                    break;
            }
        }
    }
}

