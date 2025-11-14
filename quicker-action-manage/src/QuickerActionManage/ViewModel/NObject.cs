using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickerActionManage.ViewModel
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public abstract partial class NObject : ObservableObject
    {
        [Browsable(false)]
        [JsonIgnore]
        public virtual string Summary => this.ToString();
    }
}

