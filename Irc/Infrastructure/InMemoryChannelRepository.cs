using System;
using Irc.Objects.Channel;

namespace Irc.Infrastructure;

public static class InMemoryChannelRepository
{
    private static readonly List<InMemoryChannel> _channels = new();

    public static void Add(InMemoryChannel channel)
    {
        if (
            _channels.Any(c =>
                c.ChannelName.Equals(channel.ChannelName, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            throw new InvalidOperationException($"Channel {channel.ChannelName} already exists.");
        }

        _channels.Add(channel);
    }

    public static IEnumerable<InMemoryChannel> GetAllChannels() => _channels.AsReadOnly();

    public static InMemoryChannel? GetByName(string channelName) =>
        _channels.FirstOrDefault(c =>
            c.ChannelName.Equals(channelName, StringComparison.OrdinalIgnoreCase)
        );

    public static void Remove(string channelName)
    {
        var channel = GetByName(channelName);

        if (channel != null)
        {
            _channels.Remove(channel);
        }
    }
}
