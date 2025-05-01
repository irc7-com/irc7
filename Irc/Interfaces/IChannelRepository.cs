using System;
using Irc.Objects.Channel;

namespace Irc.Interfaces;

public interface IChannelRepository
{
    InMemoryChannel? GetByName(string channelName);
    IEnumerable<InMemoryChannel> GetAllChannels();
    void Add(InMemoryChannel channel);
    void Remove(string channelName);
}
