using Irc.Constants;
using Irc.Interfaces;
using Irc.Modes.User;
using Irc.Objects.Collections;

namespace Irc.Objects.User;

public class UserModes : ModeCollection, IModeCollection, IUserModes
{
    public Oper Oper { get; } = new();
    public Invisible Invisible { get; } = new();
    public Secure Secure { get; } = new();
    public Admin Admin { get; } = new();
    public Isircx Isircx { get; } = new();
    public Gag Gag { get; } = new();
    public Host Host { get; } = new();
    
    
    public UserModes()
    {
        // IRC Modes
        Modes.Add(Resources.UserModeOper, Oper);
        Modes.Add(Resources.UserModeInvisible, Invisible);
        Modes.Add(Resources.UserModeSecure, Secure);
        //modes.Add(Resources.UserModeServerNotice, new Modes.User.ServerNotice());
        //modes.Add(Resources.UserModeWallops, new Modes.User.WallOps());

        // IRCX Modes
        Modes.Add(Resources.UserModeAdmin, new Admin());
        Modes.Add(Resources.UserModeIrcx, new Modes.User.Isircx());
        Modes.Add(Resources.UserModeGag, new Gag());

        // Apollo Modes
        Modes.Add(Resources.UserModeHost, new Host());
    }
}