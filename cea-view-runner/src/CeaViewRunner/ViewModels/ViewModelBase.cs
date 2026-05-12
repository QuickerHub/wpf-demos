using CommunityToolkit.Mvvm.ComponentModel;
using CeaViewRunner.Infrastructure;
using Newtonsoft.Json;

namespace CeaViewRunner.ViewModels;

public class ViewModelBase : ObservableObject
{
    public void MergeFromObj(object? obj)
    {
        if (obj == null)
        {
            return;
        }

        MergeFromJson(obj.ToJson());
    }

    public void MergeFromJson(string json) => JsonConvert.PopulateObject(json, this);

    public string? Result { get; set; }
}
