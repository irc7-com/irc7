namespace Irc.Interfaces;

public interface IDataStore
{
    void SetId(string id);
    void Set(string key, string value);
    void SetAs<T>(string key, T value);
    string Get(string key);
    T? GetAs<T>(string key) where T : new();
    List<KeyValuePair<string, string>> GetList();
    string GetName();
}