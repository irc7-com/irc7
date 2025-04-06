using Irc.Constants;
using Irc.Objects;
using Irc.Objects.User;

namespace Irc.Extensions.Objects.User;

public class ExtendedUserModes : UserModes
{
    public ExtendedUserModes()
    {
        Modes.Add(Resources.UserModeAdmin, new Modes.User.Admin());
        Modes.Add(ExtendedResources.UserModeIrcx, new Modes.User.Isircx());
        Modes.Add(ExtendedResources.UserModeGag, new Modes.User.Gag());
    }
}