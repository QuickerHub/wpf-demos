using Newtonsoft.Json;
using System;

namespace Cea.Utils.Extension
{
    public static class JsonExtensions
    {
        public static string ToJson(this object? obj, bool indent = false, bool ignoreNull = false)
        {
            if (obj == null) return "";
            Formatting formatting = indent ? Formatting.Indented : Formatting.None;
            JsonSerializerSettings? settings = ignoreNull ? new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Ignore nested loops
            } : null;
            return JsonConvert.SerializeObject(obj, formatting, settings);
        }

        /// <summary>
        /// Only public properties can be serialized.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns>Returns null if unsuccessful</returns>
        public static T TryJsonToObject<T>(this string source) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(source);
            }
            catch
            {
                return default;
            }
        }
    }
}

