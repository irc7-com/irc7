using Irc.Constants;
using Irc.Modes.User;
using Irc.Objects.Collections;

namespace Irc.Objects;

public class UserModes : ModeCollection, IModeCollection
{
    public UserModes()
    {
        Modes.Add(Resources.UserModeOper, new Oper());
        Modes.Add(Resources.UserModeInvisible, new Invisible());
        Modes.Add(Resources.UserModeSecure, new Secure());
        //modes.Add(Resources.UserModeServerNotice, new Modes.User.ServerNotice());
        //modes.Add(Resources.UserModeWallops, new Modes.User.WallOps());
    }

    public bool Oper
    {
        get => Modes[Resources.UserModeOper].Get() == 1;
        set => Modes[Resources.UserModeOper].Set(Convert.ToInt32(value));
    }

    public bool Invisible
    {
        get => Modes[Resources.UserModeInvisible].Get() == 1;
        set => Modes[Resources.UserModeInvisible].Set(Convert.ToInt32(value));
    }

    public bool Secure
    {
        get => Modes[Resources.UserModeSecure].Get() == 1;
        set => Modes[Resources.UserModeSecure].Set(Convert.ToInt32(value));
    }
}