using Irc.Constants;

namespace Irc.Props.Channel;

internal class OID : PropRule
{
    public OID() : base(Resources.ChannelPropOID, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, Resources.ChannelPropOIDRegex, "0", true)
    {
    }
}