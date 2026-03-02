using Irc.Interfaces;

namespace Irc.Objects.Channel;

public interface IChannelProps : IPropCollection
{
    PropRule Oid { get; }
    PropRule Name { get; }
    PropRule Creation { get; }
    PropRule Category { get; }
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
}