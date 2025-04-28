using Irc.Commands;
using Irc.Constants;
using Irc.Interfaces;
using Irc.Modes.User;
using Irc.Objects.Collections;

namespace Irc.Objects.User;

public class UserModes : ModeCollection, IModeCollection, IUserModes
{
    public OperRule Oper { get; } = new();
    public InvisibleRule Invisible { get; } = new();
    public SecureRule Secure { get; } = new();
    public AdminRule Admin { get; } = new();
    public Isircx Isircx { get; } = new();
    public GagRule Gag { get; } = new();
    public HostRule Host { get; } = new();
    
    
    public UserModes()
    {
        // IRC Modes
        Modes.Add(Resources.UserModeOper, Oper);
        Modes.Add(Resources.UserModeInvisible, Invisible);
        Modes.Add(Resources.UserModeSecure, Secure);
        //modes.Add(Resources.UserModeServerNotice, new Modes.User.ServerNotice());
        //modes.Add(Resources.UserModeWallops, new Modes.User.WallOps());

        // IRCX Modes
        Modes.Add(Resources.UserModeAdmin, new AdminRule());
        Modes.Add(Resources.UserModeIrcx, new Modes.User.IsIrcxRule());
        Modes.Add(Resources.UserModeGag, new GagRule());

        // Apollo Modes
        Modes.Add(Resources.UserModeHost, new HostRule());
    }
}