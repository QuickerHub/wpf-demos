using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using QuickerActionManage.Utils;

namespace QuickerActionManage.ViewModel
{
    public abstract class ListModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

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

