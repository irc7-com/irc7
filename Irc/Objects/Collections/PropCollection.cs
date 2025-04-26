using Irc.Constants;
using Irc.Interfaces;

namespace Irc.Objects.Collections;

public class PropCollection : IPropCollection
{
    protected Dictionary<string, IPropRule> properties = new(StringComparer.InvariantCultureIgnoreCase);
    
    public PropRule Oid { get; } = new(
        Resources.ChannelPropOID, 
        EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, 
        Resources.ChannelPropOIDRegex,
        "0", 
        true
    );

    public PropCollection()
    {
        AddProp(Oid);
    }
    
    public string this[string key] => properties.ContainsKey(key) ? properties[key].Value : string.Empty;
    
    public IPropRule? GetProp(string name)
    {
        properties.TryGetValue(name, out var rule);
        return rule;
    }

    public List<IPropRule> GetProps()
    {
        return properties.Values.ToList();
    }

    public void AddProp(IPropRule prop)
    {
        properties[prop.Name.ToUpper()] = prop;
    }

    public void SetProp(string name, string value)
    {
        properties[name].SetValue(value);
    }
}