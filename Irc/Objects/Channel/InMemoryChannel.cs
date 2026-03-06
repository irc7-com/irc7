using System;

namespace Irc.Objects.Channel;

public class InMemoryChannel
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string ServerName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelTopic { get; set; } = string.Empty;
    public string Modes { get; set; } = string.Empty;
    public int UserLimit { get; set; } = 50;
    public string Locale { get; set; } = string.Empty;
    public int Language { get; set; } = 1;
    public string OwnerKey { get; set; } = string.Empty;
    public string HostKey { get; set; } = string.Empty;
    public int LRSID { get; set; } = 0;
}
