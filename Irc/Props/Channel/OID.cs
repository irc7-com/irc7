using Irc.Constants;

namespace Irc.Props.Channel;

internal class OID : PropRule
{
    public OID() : base(ExtendedResources.ChannelPropOID, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, Resources.ChannelPropOIDRegex, "0", true)
    {
    }
}