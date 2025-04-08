using Irc.Interfaces;

namespace Irc.Objects.Collections;

public class PropCollection : IPropCollection
{
    protected Dictionary<string, IPropRule> properties = new(StringComparer.InvariantCultureIgnoreCase);

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