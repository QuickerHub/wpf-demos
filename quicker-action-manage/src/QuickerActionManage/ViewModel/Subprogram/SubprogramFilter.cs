using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickerActionManage.Utils;

namespace QuickerActionManage.ViewModel
{
    public partial class SubprogramFilter : NObject
    {
        [Browsable(false)]
        [ObservableProperty]
        public partial string? SearchText { get; set; }

        public override string Summary => "";
        public bool Filter(SubprogramModel item)
        {
            return TextUtil.Search(SearchText, item.Name, item.Description);
        }
    }
}

