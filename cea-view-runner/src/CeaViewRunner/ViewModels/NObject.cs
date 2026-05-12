using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace CeaViewRunner.ViewModels;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public abstract class NObject : ObservableObject
{
    [Browsable(false)]
    [JsonIgnore]
    public virtual string Summary => ToString() ?? "";
}
