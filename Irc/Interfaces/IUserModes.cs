using Irc.Commands;
using Irc.Modes.User;

namespace Irc.Interfaces;

public interface IUserModes: IModeCollection
{
    OperRule Oper { get; }
    InvisibleRule Invisible { get; }
    SecureRule Secure { get; }
    AdminRule Admin { get; }
    Isircx Isircx { get; }
    GagRule Gag { get; }
    HostRule Host { get; }
}