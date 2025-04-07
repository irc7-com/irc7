using Irc;
using Irc.Constants;
using Irc.Objects.User;

public class ExtendedUserModes : UserModes
{
    public ExtendedUserModes()
    {
        Modes.Add(Resources.UserModeAdmin, new Admin());
        Modes.Add(ExtendedResources.UserModeIrcx, new Irc.Modes.User.Isircx());
        Modes.Add(ExtendedResources.UserModeGag, new Gag());
    }
}