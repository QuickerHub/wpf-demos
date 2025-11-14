using System.Collections.Generic;
using System.ComponentModel;

namespace QuickerActionManage.ViewModel
{
    public abstract class Sorter : NObject
    {
        public abstract IEnumerable<SortDescription> GetSortDescription();
    }
}

