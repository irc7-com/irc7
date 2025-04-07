using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Server;

namespace Irc.Props.User;

public class SubscriberInfo : PropRule
{
    private readonly ApolloServer _apolloServer;

    public SubscriberInfo(ApolloServer apolloServer) : base(Resources.UserPropSubscriberInfo,
        EnumChannelAccessLevel.None, EnumChannelAccessLevel.ChatMember, Resources.GenericProps, "0", true)
    {
        _apolloServer = apolloServer;
    }

    public override EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        _apolloServer.ProcessCookie((IUser)source, Resources.UserPropSubscriberInfo, propValue);
        return EnumIrcError.NONE;
    }
}