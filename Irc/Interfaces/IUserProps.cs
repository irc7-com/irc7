using Irc.Interfaces;

namespace Irc.Objects.User;

public interface IUserProps : IPropCollection
{
    PropNick Nick { get; }
    PropSubInfo SubscriberInfo { get; }
    PropProfile Profile { get; }
    PropRole Role { get; }
    PropRule Oid { get; }
}