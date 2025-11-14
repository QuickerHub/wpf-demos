using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace QuickerActionManage.ViewModel
{
    public abstract class Sorter : NObject
    {
        public abstract IEnumerable<SortDescription> GetSortDescription();
    }
}

