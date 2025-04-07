using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Props.Channel;

public class Topic : PropRule
{
    public Topic() : base(ExtendedResources.ChannelPropTopic, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.ChatHost, Resources.ChannelPropTopicRegex, string.Empty)
    {
    }

    public override string GetValue(IChatObject target)
    {
        return ((IChannel)target).ChannelStore.Get("topic");
    }

    public override EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        var result = base.EvaluateSet(source, target, propValue);
        if (result != EnumIrcError.OK) return result;

        var channel = (IChannel)target;
        channel.ChannelStore.Set("topic", propValue);
        return result;
    }
}