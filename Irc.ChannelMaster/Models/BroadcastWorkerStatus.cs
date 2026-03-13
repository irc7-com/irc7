namespace Irc.ChannelMaster.Models;

public sealed class BroadcastWorkerStatus
{
    public required string WorkerId { get; init; }
    public required int CurrentLoad { get; init; }
    public required DateTime LastSeenUtc { get; init; }
}

