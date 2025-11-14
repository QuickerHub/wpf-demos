using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickerActionManage.Utils;

namespace QuickerActionManage.ViewModel
{
    public abstract partial class ListModel : ObservableObject
    {
        protected readonly DebounceTimer _debounce = new();
        protected abstract CollectionView GetView();
        protected abstract IEnumerable<SortDescription> GetSortDescriptions();
        public void Refresh() => _debounce.DoAction(() => GetView().Refresh());
        public void ReSort()
        {
            GetView().SortDescriptions.Clear();
            foreach (var item in GetSortDescriptions())
            {
                GetView().SortDescriptions.Add(item);
            }
            Refresh();
        }
    }
}

