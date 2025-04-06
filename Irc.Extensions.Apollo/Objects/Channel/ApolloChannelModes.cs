using Irc.Extensions.Apollo.Interfaces;
using Irc.Extensions.Apollo.Modes.Channel;
using Irc.Extensions.Modes.Channel;
using Irc.Extensions.Objects.Channel;

namespace Irc.Extensions.Apollo.Objects.Channel;

public class ApolloChannelModes : ExtendedChannelModes, IApolloChannelModes
{
    public ApolloChannelModes()
    {
        Modes.Add(ExtendedResources.ChannelModeNoGuestWhisper, new NoGuestWhisper());
        Modes.Add(ApolloResources.ChannelModeOnStage, new OnStage());
        Modes.Add(ApolloResources.ChannelModeSubscriber, new Subscriber());
    }

    public bool OnStage
    {
        get => Modes[ApolloResources.ChannelModeOnStage].Get() == 1;
        set => Modes[ApolloResources.ChannelModeOnStage].Set(Convert.ToInt32(value));
    }

    public bool Subscriber
    {
        get => Modes[ApolloResources.ChannelModeSubscriber].Get() == 1;
        set => Modes[ApolloResources.ChannelModeSubscriber].Set(Convert.ToInt32(value));
    }
}