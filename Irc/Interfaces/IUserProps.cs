using Irc.Interfaces;

namespace Irc.Objects.User;

public interface IUserProps
{
    PropNick Nick { get; }
    PropSubInfo SubscriberInfo { get; }
    PropProfile Profile { get; }
    PropRole Role { get; }
    PropRule Oid { get; }
    string this[string key] { get; }
    IPropRule? GetProp(string name);
    List<IPropRule> GetProps();
    void AddProp(IPropRule prop);
    void SetProp(string name, string value);
}