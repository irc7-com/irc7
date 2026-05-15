using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Irc.Interfaces;

namespace Irc.IO;

public class DataStore : IDataStore
{
    private readonly bool _persist;
    private readonly string _section = string.Empty;
    private readonly Dictionary<string, string> _sets = new(StringComparer.InvariantCultureIgnoreCase);
    private string _id = string.Empty;

    public DataStore(string id, string section, bool persist = true)
    {
        _id = id;
        _section = section;
        _persist = persist;
    }

    public DataStore(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            // Use source-generated context to avoid reflection (AOT-safe)
            var tempSet = JsonSerializer.Deserialize(File.ReadAllText(path),
                IrcJsonContext.Default.DictionaryStringString);
            if (tempSet == null) return;

            foreach (var kvp in tempSet) _sets.Add(kvp.Key, kvp.Value);
        }
    }

    public void SetId(string id)
    {
        _id = id;
    }

    public void Set(string key, string value)
    {
        _sets[key] = value;
    }

    [RequiresUnreferencedCode("Generic JSON serialization requires reflection. Use SetAs<T>(key, value, JsonTypeInfo<T>) for AOT.")]
    [RequiresDynamicCode("Generic JSON serialization requires dynamic code. Use SetAs<T>(key, value, JsonTypeInfo<T>) for AOT.")]
    public void SetAs<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        Set(key, json);
    }

    /// <summary>AOT-safe overload that accepts a <see cref="JsonTypeInfo{T}"/>.</summary>
    public void SetAs<T>(string key, T value, JsonTypeInfo<T> typeInfo)
    {
        var json = JsonSerializer.Serialize(value, typeInfo);
        Set(key, json);
    }

    public string Get(string key)
    {
        _sets.TryGetValue(key, out var value);
        return value ?? string.Empty;
    }

    [RequiresUnreferencedCode("Generic JSON deserialization requires reflection. Use GetAs<T>(key, JsonTypeInfo<T>) for AOT.")]
    [RequiresDynamicCode("Generic JSON deserialization requires dynamic code. Use GetAs<T>(key, JsonTypeInfo<T>) for AOT.")]
    public T GetAs<T>(string key) where T : new()
    {
        var json = Get(key);
        if (string.IsNullOrEmpty(json)) return new T();
        return JsonSerializer.Deserialize<T>(json) ?? new T();
    }

    /// <summary>AOT-safe overload that accepts a <see cref="JsonTypeInfo{T}"/>.</summary>
    public T? GetAs<T>(string key, JsonTypeInfo<T> typeInfo)
    {
        var json = Get(key);
        if (string.IsNullOrEmpty(json)) return default;
        return JsonSerializer.Deserialize(json, typeInfo);
    }

    public List<KeyValuePair<string, string>> GetList()
    {
        return _sets.ToList();
    }

    public string GetName()
    {
        return _section;
    }
}