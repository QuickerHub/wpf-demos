using System;
using System.Collections.Generic;

namespace QuickerExpressionAgent.Common;

/// <summary>
/// JSON serialization/deserialization extension methods
/// Uses Newtonsoft.Json for net472 and System.Text.Json for net8.0
/// </summary>
public static class JsonExtensions
{
#if NET472
    // Use Newtonsoft.Json for net472
    /// <summary>
    /// Serialize object to JSON string
    /// </summary>
    public static string ToJson<T>(this T? value, bool indented = true)
    {
        if (value == null)
        {
            return "null";
        }

        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            Formatting = indented ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include,
            StringEscapeHandling = Newtonsoft.Json.StringEscapeHandling.Default // Default behavior preserves Chinese characters
        };

        return Newtonsoft.Json.JsonConvert.SerializeObject(value, settings);
    }

    /// <summary>
    /// Deserialize JSON string to object
    /// </summary>
    public static T? FromJson<T>(this string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
        catch
        {
            return default;
        }
    }
#else
    // Use System.Text.Json for net8.0
    /// <summary>
    /// Serialize object to JSON string
    /// </summary>
    public static string ToJson<T>(this T? value, bool indented = true)
    {
        if (value == null)
        {
            return "null";
        }

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = indented,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
        };

        return System.Text.Json.JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserialize JSON string to object
    /// </summary>
    public static T? FromJson<T>(this string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }
#endif
}

