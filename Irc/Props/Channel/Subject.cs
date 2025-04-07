using Irc.Constants;

namespace Irc.Props.Channel;

internal class Subject : PropRule
{
    public Subject() : base(Resources.ChannelPropSubject, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, Resources.GenericProps, string.Empty, true)
    {
    }
}