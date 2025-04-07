using Irc.Constants;

namespace Irc.Props.Channel;

internal class ServicePath : PropRule
{
    public ServicePath() : base(ExtendedResources.ChannelPropServicePath, EnumChannelAccessLevel.None,
        EnumChannelAccessLevel.ChatOwner, Resources.GenericProps, string.Empty, true)
    {
    }
}