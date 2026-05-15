using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace Irc.Interfaces;

public interface IDataStore
{
    void SetId(string id);
    void Set(string key, string value);

    [RequiresUnreferencedCode("Use SetAs<T>(key, value, JsonTypeInfo<T>) for AOT.")]
    [RequiresDynamicCode("Use SetAs<T>(key, value, JsonTypeInfo<T>) for AOT.")]
    void SetAs<T>(string key, T value);

    void SetAs<T>(string key, T value, JsonTypeInfo<T> typeInfo);

    string Get(string key);

    [RequiresUnreferencedCode("Use GetAs<T>(key, JsonTypeInfo<T>) for AOT.")]
    [RequiresDynamicCode("Use GetAs<T>(key, JsonTypeInfo<T>) for AOT.")]
    T? GetAs<T>(string key) where T : new();

    T? GetAs<T>(string key, JsonTypeInfo<T> typeInfo);

    List<KeyValuePair<string, string>> GetList();
    string GetName();
}