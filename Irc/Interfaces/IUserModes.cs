using Irc.Modes.User;

namespace Irc.Interfaces;

public interface IUserModes: IModeCollection
{
    Oper Oper { get; }
    Invisible Invisible { get; }
    Secure Secure { get; }
    Admin Admin { get; }
    Isircx Isircx { get; }
    Gag Gag { get; }
    Host Host { get; }
}