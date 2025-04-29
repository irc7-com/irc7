using System;

namespace Irc.Objects.Channel;

public class InMemoryChannel
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Category { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelTopic { get; set; } = string.Empty;
    public string Modes { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string OwnerKey { get; set; } = string.Empty;
    public int Unknown { get; set; } = 0;
}
