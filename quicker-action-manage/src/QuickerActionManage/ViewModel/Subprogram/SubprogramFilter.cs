using System.ComponentModel;
using QuickerActionManage.Utils;

namespace QuickerActionManage.ViewModel
{
    public class SubprogramFilter : NObject
    {
        [Browsable(false)]
        public string? SearchText { get; set; }

        public override string Summary => "";
        public bool Filter(SubprogramModel item)
        {
            return TextUtil.Search(SearchText, item.Name, item.Description);
        }
    }
}

