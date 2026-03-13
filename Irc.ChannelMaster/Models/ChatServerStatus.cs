namespace Irc.ChannelMaster.Models;

public sealed class ChatServerStatus
{
    public required string ChatServerId { get; init; }
    public required int CurrentLoad { get; init; }
    public required DateTime LastSeenUtc { get; init; }
}

