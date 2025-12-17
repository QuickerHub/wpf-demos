using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// Helper class for JSON operations
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Remove null properties from JSON string
        /// </summary>
        /// <param name="json">Original JSON string</param>
        /// <returns>JSON string with null properties removed, or null if parsing fails</returns>
        public static string? RemoveNullProperties(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            try
            {
                var token = JToken.Parse(json);
                RemoveNullProperties(token);
                return token.ToString(Formatting.None);
            }
            catch
            {
                // If parsing fails, return original JSON
                return json;
            }
        }

        /// <summary>
        /// Recursively remove null properties from JToken
        /// </summary>
        private static void RemoveNullProperties(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                var propertiesToRemove = new List<string>();

                foreach (var property in obj.Properties())
                {
                    if (property.Value.Type == JTokenType.Null)
                    {
                        propertiesToRemove.Add(property.Name);
                    }
                    else
                    {
                        RemoveNullProperties(property.Value);
                    }
                }

                foreach (var propertyName in propertiesToRemove)
                {
                    obj.Remove(propertyName);
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                var array = (JArray)token;
                foreach (var item in array)
                {
                    RemoveNullProperties(item);
                }
            }
        }
    }
}

