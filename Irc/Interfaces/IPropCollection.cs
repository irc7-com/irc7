namespace Irc.Interfaces;

public interface IPropCollection
{
    public IPropRule? GetProp(string name);
    public List<IPropRule> GetProps();
    public void SetProp(string name, string value);
}