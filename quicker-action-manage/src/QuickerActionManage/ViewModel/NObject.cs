using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace QuickerActionManage.ViewModel
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public abstract class NObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        [Browsable(false)]
        [JsonIgnore]
        public virtual string Summary => this.ToString();
    }
}

