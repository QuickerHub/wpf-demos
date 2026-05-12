using Newtonsoft.Json;

namespace CeaViewRunner.Infrastructure;

/// <summary>
/// JSON helpers matching Cea.Utils.Extension usage from ViewRunner.
/// </summary>
public static class JsonNetExtensions
{
    public static string ToJson(this object? obj) => JsonConvert.SerializeObject(obj);

    public static T? TryJsonToObject<T>(this string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(json!);
        }
        catch
        {
            return null;
        }
    }
}
