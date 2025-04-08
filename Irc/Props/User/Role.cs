using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Props.User;

public class Role : PropRule
{
    private readonly IServer _server;

    public Role(IServer server) : base(Resources.UserPropRole, EnumChannelAccessLevel.None,
        EnumChannelAccessLevel.ChatMember, Resources.GenericProps, "0", true)
    {
        _server = server;
    }

    public override EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        _server.ProcessCookie((IUser)source, Resources.UserPropRole, propValue);
        return EnumIrcError.NONE;
    }
}