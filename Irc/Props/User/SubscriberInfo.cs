using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Props.User;

public class SubscriberInfo : PropRule
{
    private readonly IServer _server;

    public SubscriberInfo(IServer server) : base(Resources.UserPropSubscriberInfo,
        EnumChannelAccessLevel.None, EnumChannelAccessLevel.ChatMember, Resources.GenericProps, "0", true)
    {
        _server = server;
    }

    public override EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        _server.ProcessCookie((IUser)source, Resources.UserPropSubscriberInfo, propValue);
        return EnumIrcError.NONE;
    }
}