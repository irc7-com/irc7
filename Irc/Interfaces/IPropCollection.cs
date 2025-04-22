namespace Irc.Interfaces;

public interface IPropCollection
{
    IPropRule? GetProp(string name);
    List<IPropRule> GetProps();
    void SetProp(string name, string value);
}