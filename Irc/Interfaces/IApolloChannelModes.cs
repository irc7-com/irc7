namespace Irc.Interfaces;

public interface IApolloChannelModes : IChannelModes
{
    bool OnStage { get; set; }
    bool Subscriber { get; set; }
}