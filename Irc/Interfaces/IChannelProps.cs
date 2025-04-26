using Irc.Interfaces;

namespace Irc.Objects.Channel;

public interface IChannelProps : IPropCollection
{
    PropRule Oid { get; }
    PropRule Name { get; }
    PropRule Creation { get; }
    PropRule Language { get; }
    PropRule OwnerKey { get; }
    PropRule HostKey { get; }
    Memberkey MemberKey { get; }
    PropRule Pics { get; }
    Topic Topic { get; }
    PropRule Subject { get; }
    PropRule Onjoin { get; }
    PropRule Onpart { get; }
    PropRule Lag { get; }
    PropRule Client { get; }
    PropRule ClientGUID { get; }
    PropRule ServicePath { get; }
    PropRule Account { get; }
    string this[string key] { get; }
    IPropRule? GetProp(string name);
    List<IPropRule> GetProps();
    void AddProp(IPropRule prop);
    void SetProp(string name, string value);
}