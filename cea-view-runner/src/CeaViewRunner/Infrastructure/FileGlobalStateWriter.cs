using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CeaViewRunner.Infrastructure;

/// <summary>
/// Persists string key/value pairs per logical id under LocalApplicationData (standalone substitute for Quicker ActionStateWriter).
/// </summary>
public sealed class FileGlobalStateWriter
{
    private readonly string _filePath;

    public FileGlobalStateWriter(string id)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CeaViewRunner",
            "state");
        Directory.CreateDirectory(root);
        var safeId = string.Join("_", id.Split(Path.GetInvalidFileNameChars()));
        _filePath = Path.Combine(root, $"state_{safeId}.json");
    }

    public object? Read(string key, object? defaultValue = null)
    {
        var dict = ReadAll();
        return dict.TryGetValue(key, out var v) ? v : defaultValue;
    }

    public void Write(string key, object? value)
    {
        if (value == null)
        {
            return;
        }

        var str = value is string s ? s : JsonConvert.SerializeObject(value);
        var dict = ReadAll();
        dict[key] = str;
        WriteAll(dict);
    }

    public bool Remove(string key)
    {
        var dict = ReadAll();
        if (!dict.Remove(key))
        {
            return false;
        }

        WriteAll(dict);
        return true;
    }

    public void Delete()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch
        {
            // ignore
        }
    }

    private Dictionary<string, string> ReadAll()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            var text = File.ReadAllText(_filePath);
            var token = JToken.Parse(text);
            if (token is not JObject obj)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var p in obj.Properties())
            {
                d[p.Name] = p.Value.Type == JTokenType.String ? p.Value.ToString() : p.Value.ToString(Formatting.None);
            }

            return d;
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private void WriteAll(Dictionary<string, string> dict)
    {
        var obj = new JObject();
        foreach (var kv in dict)
        {
            obj[kv.Key] = kv.Value;
        }

        File.WriteAllText(_filePath, obj.ToString(Formatting.Indented));
    }
}
